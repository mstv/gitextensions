using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CommonTestUtils;
using GitUI;
using NUnit.Framework;

namespace GitUITests
{
    public static class UITest
    {
        public static async Task WaitForIdleAsync()
        {
            var idleCompletionSource = new TaskCompletionSource<VoidResult>();
            Application.Idle += HandleApplicationIdle;

            // Queue an event to make sure we don't stall if the application was already idle
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            await Task.Yield();

            await idleCompletionSource.Task;
            Application.Idle -= HandleApplicationIdle;

            void HandleApplicationIdle(object sender, EventArgs e)
            {
                idleCompletionSource.TrySetResult(default);
            }
        }

        public static void RunForm<T>(
            Action showForm,
            Func<T, Task> runAsync)
            where T : Form
        {
            // Start runAsync before calling showForm.
            // The latter might block until the form is closed, especially if using Application.Run(form).

            // Avoid using ThreadHelper.JoinableTaskFactory for the outermost operation because we don't want the task
            // tracked by its collection. Otherwise, test code would not be able to wait for pending operations to
            // complete.
            var test = ThreadHelper.JoinableTaskContext.Factory.RunAsync(async () =>
            {
                // Wait for the form to be opened by the test thread.
                await WaitForIdleAsync();
                var form = Application.OpenForms.OfType<T>().Single();
                Assert.IsTrue(form.Visible);

                // Wait for potential pending asynchronous tasks triggered by the form.
                AsyncTestHelper.WaitForPendingOperations(AsyncTestHelper.UnexpectedTimeout);

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                try
                {
                    await runAsync(form);
                }
                finally
                {
                    form.Close(); // also calls form.Dispose()
                }
            });

            showForm();

            // Join the asynchronous test operation so any exceptions are rethrown on this thread
            test.Join();
        }

        private readonly struct VoidResult
        {
        }
    }
}
