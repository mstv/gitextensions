#nullable enable

using System;
using System.Text;
using System.Windows.Forms;
using GitCommands;
using GitUI;
using GitUI.NBugReports;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace GitExtensions
{
    public static class BugReporter
    {
        private const string _separator = ": ";

        private static Form? OwnerForm
            => Form.ActiveForm ?? (Application.OpenForms.Count > 0 ? Application.OpenForms[0] : null);

        private static IntPtr OwnerFormHandle
            => OwnerForm?.Handle ?? IntPtr.Zero;

        /// <summary>
        /// Appends the exception data and gets the root error.
        /// </summary>
        /// <param name="text">A StringBuilder to which the exception data is appended.</param>
        /// <param name="exception">An Exception to describe.</param>
        /// <returns>The inner-most exception message.</returns>
        internal static string Append(StringBuilder text, Exception exception)
        {
            string rootError = exception.Message;

            if (exception.InnerException is not null)
            {
                int prevLength = text.Length;

                rootError = Append(text, exception.InnerException);

                // if text was added, append a new line
                if (prevLength < text.Length)
                {
                    text.AppendLine();
                }

                // append the exception message if different
                if (exception.Message != exception.InnerException.Message)
                {
                    text.AppendLine(exception.Message);
                }
            }

            if (exception is UserExternalOperationException userExternalOperationException)
            {
                // Operation: <context>
                if (!string.IsNullOrWhiteSpace(userExternalOperationException.Context))
                {
                    text.Append(Strings.Operation).Append(_separator).AppendLine(userExternalOperationException.Context);
                }
            }

            if (exception is ExternalOperationException externalOperationException)
            {
                // Command: <command>
                if (!string.IsNullOrWhiteSpace(externalOperationException.Command))
                {
                    text.Append(Strings.Command).Append(_separator).AppendLine(externalOperationException.Command);
                }

                // Arguments: <args>
                if (!string.IsNullOrWhiteSpace(externalOperationException.Arguments))
                {
                    text.Append(Strings.Arguments).Append(_separator).AppendLine(externalOperationException.Arguments);
                }

                // Working directory: <working dir>
                text.Append(Strings.WorkingDirectory).Append(_separator).AppendLine(externalOperationException.WorkingDirectory);
            }

            return rootError;
        }

        public static void Report(Exception exception, bool isTerminating)
        {
            bool isUserExternalOperation = exception is UserExternalOperationException;
            bool isExternalOperation = exception is ExternalOperationException;

            StringBuilder text = new();
            string rootError = Append(text, exception);

            using var taskDialog = new TaskDialog
            {
                OwnerWindowHandle = OwnerFormHandle,
                Icon = TaskDialogStandardIcon.Error,
                Caption = Strings.Error,
                InstructionText = rootError,
                Cancelable = true,
            };

            // prefer to ignore failed external operations
            if (isExternalOperation)
            {
                AddIgnoreOrCloseButton();
            }

            // no bug reports for user configured operations
            if (!isUserExternalOperation)
            {
                // directions and button to raise a bug
                text.AppendLine().AppendLine(Strings.ReportBug);
                taskDialog.AddButton(Strings.ButtonReportBug, () => ShowNBug(OwnerForm, exception, isTerminating));
            }

            // let the user decide whether to report the bug
            if (!isExternalOperation)
            {
                AddIgnoreOrCloseButton();
            }

            taskDialog.Text = text.ToString();
            taskDialog.Show();
            return;

            void AddIgnoreOrCloseButton()
            {
                taskDialog.AddButton(isTerminating ? Strings.ButtonCloseApp : Strings.ButtonIgnore);
            }
        }

        private static void ShowNBug(IWin32Window? owner, Exception exception, bool isTerminating)
        {
            var envInfo = UserEnvironmentInformation.GetInformation();

            using (var form = new GitUI.NBugReports.BugReportForm())
            {
                var result = form.ShowDialog(owner, exception, envInfo);
                if (isTerminating || result == DialogResult.Abort)
                {
                    Environment.Exit(-1);
                }
            }
        }
    }
}
