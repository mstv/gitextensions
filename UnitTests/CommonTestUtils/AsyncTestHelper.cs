﻿using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using GitUI;

namespace CommonTestUtils
{
    public static class AsyncTestHelper
    {
        public static TimeSpan UnexpectedTimeout
        {
            get
            {
                return Debugger.IsAttached ? TimeSpan.FromHours(1) : TimeSpan.FromMinutes(1);
            }
        }

        public static void RunAndWaitForPendingOperations(Func<Task> asyncMethod, CancellationToken cancellationToken)
        {
            if (ThreadHelper.JoinableTaskFactory == null)
            {
                throw new InvalidOperationException("Run & wait is pointless without JoinableTaskContext.");
            }

            ThreadHelper.JoinableTaskFactory.Run(asyncMethod);

            WaitForPendingOperations(cancellationToken);
        }

        public static T RunAndWaitForPendingOperations<T>(Func<Task<T>> asyncMethod, CancellationToken cancellationToken)
        {
            if (ThreadHelper.JoinableTaskFactory == null)
            {
                throw new InvalidOperationException("Run & wait is pointless without JoinableTaskContext.");
            }

            var result = ThreadHelper.JoinableTaskFactory.Run(asyncMethod);

            WaitForPendingOperations(cancellationToken);

            return result;
        }

        public static void WaitForPendingOperations(TimeSpan timeout)
        {
            using (var cts = new CancellationTokenSource(timeout))
            {
                do
                {
                    try
                    {
                        WaitForPendingOperations(cts.Token);
                        return;
                    }
                    catch (OperationCanceledException ex)
                    {
                        var ctsCanceled = cts.IsCancellationRequested ? string.Empty : "not ";
                        Console.WriteLine(
                            $"{nameof(WaitForPendingOperations)} {nameof(OperationCanceledException)} cts {ctsCanceled}canceled, ex {ex.Demystify()}");
                    }
                }
                while (!cts.IsCancellationRequested);
            }
        }

        public static void WaitForPendingOperations(CancellationToken cancellationToken)
        {
            if (ThreadHelper.JoinableTaskContext == null)
            {
                throw new InvalidOperationException("Wait is pointless without JoinableTaskContext.");
            }

            ThreadHelper.JoinableTaskContext.Factory.Run(() => ThreadHelper.JoinPendingOperationsAsync(cancellationToken));
        }
    }
}
