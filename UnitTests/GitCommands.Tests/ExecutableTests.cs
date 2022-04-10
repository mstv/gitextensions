using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FluentAssertions;
using GitCommands;
using GitExtUtils;
using GitUI;
using GitUIPluginInterfaces;
using Microsoft.VisualStudio.Threading;
using NUnit.Framework;

namespace GitCommandsTests
{
    public sealed class ExecutableTests
    {
        [SetUp]
        public void SetUp()
        {
        }

        [TearDown]
        public void TearDown()
        {
        }

        [Test]
        public async Task WaitForProcessExitAsync_shall_return_latest_after_timeout()
        {
            TimeSpan halfRuntime = TimeSpan.FromSeconds(3);

            await TaskScheduler.Default;

            // start a process running for seconds
            IExecutable executable = new Executable("ping.exe");
            using IProcess process = executable.Start($"-n {(halfRuntime.TotalSeconds * 2) + 1} 127.0.0.1");

            // wait for process exit, but cancel the wait while the process is still running
            using CancellationTokenSource cts = new();
            DateTime startedAt = DateTime.Now;
            cts.CancelAfter(halfRuntime);
            try
            {
                await process.WaitForExitAsync(cts.Token); // may or may not throw on cancel
            }
            catch (OperationCanceledException)
            {
                // ignore
            }

            TimeSpan durationWaitTimeout = DateTime.Now - startedAt;
            durationWaitTimeout.Should().BeGreaterThan(0.5 * halfRuntime).And.BeLessThan(1.5 * halfRuntime);

            // wait for process exit without cancellation
            await process.WaitForExitAsync(CancellationToken.None);

            TimeSpan durationExit = DateTime.Now - startedAt;
            durationExit.Should().BeGreaterThan(1.5 * halfRuntime).And.BeLessThan(2.5 * halfRuntime);
        }

        [Test]
        public async Task ExecuteAsync_shall_return_latest_after_timeout([Values("cmd.exe", "ping.exe")] string exeFile)
        {
            const int cancelDelay = 1000;
            const int exitDelay = IProcess.DefaultExitTimeoutMilliseconds + cancelDelay;
            const int minRuntime = cancelDelay + exitDelay;
            string arguments = exeFile.Contains("ping") ? $"-n {(minRuntime / 1000) + 2} 127.0.0.1" : "";

            CancellationTokenSequence cancellationTokenSequence = new();
            CancellationToken cancellationToken = cancellationTokenSequence.Next();
            IExecutable executable = new Executable(exeFile);

            Exception exception = null;
            ExecutionResult? executionResult = null;
            async Task ExecuteAsync()
            {
                await TaskScheduler.Default;
                try
                {
                    executionResult = await executable.ExecuteAsync(arguments, outputEncoding: Encoding.Default, cancellationToken: cancellationToken);
                }
                catch (Exception ex)
                {
                    exception = ex;
                    throw;
                }
            }

            ThreadHelper.JoinableTaskFactory.RunAsync(ExecuteAsync, JoinableTaskCreationOptions.LongRunning)
                .FileAndForget(ex => false);

            await Task.Delay(cancelDelay);
            cancellationTokenSequence.CancelCurrent();
            await Task.Delay(exitDelay);

            exception.Should().NotBeNull();
            exception.GetType().Should().BeDerivedFrom<OperationCanceledException>();
            executionResult.Should().BeNull();
        }
    }
}
