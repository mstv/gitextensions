using System.Diagnostics;
using System.Windows.Forms;
using GitCommands.Git;
using Microsoft.WindowsAPICodePack.Dialogs;
namespace GitUI
{
    public static class OsShellUtil
    {
        public static void Open(string filePath)
        {
            try
            {
                Process.Start(filePath);
            }
            catch (System.ComponentModel.Win32Exception)
            {
                OpenAs(filePath);
            }
        }

        public static void OpenAs(string filePath)
        {
            try
            {
                ExecutableFactory.Default.Create("rundll32.exe").Start("shell32.dll,OpenAs_RunDLL " + filePath);
            }
            catch (ExecutableException)
            {
                // ignore because already shown to the user
            }
        }

        public static void SelectPathInFileExplorer(string filePath)
        {
            try
            {
                ExecutableFactory.Default.Create("explorer.exe").Start("/select, " + filePath);
            }
            catch (ExecutableException)
            {
                // ignore because already shown to the user
            }
        }

        public static void OpenWithFileExplorer(string filePath)
        {
            try
            {
                ExecutableFactory.Default.Create("explorer.exe").Start(filePath);
            }
            catch (ExecutableException)
            {
                // ignore because already shown to the user
            }
        }

        /// <summary>
        /// opens urls even with anchor
        /// </summary>
        public static void OpenUrlInDefaultBrowser(string url)
        {
            if (!string.IsNullOrWhiteSpace(url))
            {
                try
                {
                    ExecutableFactory.Default.Create(url).Start(useShellExecute: true);
                }
                catch (ExecutableException)
                {
                    // ignore because already shown to the user
                }
            }
        }

        /// <summary>
        /// Prompts the user to select a directory.
        /// </summary>
        /// <param name="ownerWindow">The owner window.</param>
        /// <param name="selectedPath">The initially selected path.</param>
        /// <returns>The path selected by the user, or null if the user cancels the dialog.</returns>
        public static string PickFolder(IWin32Window ownerWindow, string selectedPath = null)
        {
            if (GitCommands.Utils.EnvUtils.IsWindowsVistaOrGreater())
            {
                // use Vista+ dialog
                using (var dialog = new CommonOpenFileDialog())
                {
                    dialog.IsFolderPicker = true;

                    if (selectedPath != null)
                    {
                        dialog.InitialDirectory = selectedPath;
                    }

                    var result = dialog.ShowDialog(ownerWindow.Handle);

                    if (result == CommonFileDialogResult.Ok)
                    {
                        return dialog.FileName;
                    }
                }
            }
            else
            {
                // use XP-era dialog
                using (var dialog = new FolderBrowserDialog())
                {
                    if (selectedPath != null)
                    {
                        dialog.SelectedPath = selectedPath;
                    }

                    var result = dialog.ShowDialog(ownerWindow);

                    if (result == DialogResult.OK)
                    {
                        return dialog.SelectedPath;
                    }
                }
            }

            // return null if the user cancelled
            return null;
        }
    }
}
