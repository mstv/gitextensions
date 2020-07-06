using System;
using System.Diagnostics;
using JetBrains.Annotations;

namespace GitCommands.Git
{
    public class ExternalOperationExceptionFactory
    {
        /// <summary>
        /// Singleton accessor.
        /// </summary>
        public static ExternalOperationExceptionFactory Default { get; } = new ExternalOperationExceptionFactory();

        public event Action<ExternalOperationException> OnException;

        public ExecutableException Create(ProcessStartInfo processStartInfo, [NotNull] Exception inner, bool invokeEvent = true)
        {
            ExecutableException executableException = new ExecutableException(processStartInfo, inner);

            if (invokeEvent)
            {
                OnException?.Invoke(executableException);
            }

            return executableException;
        }
    }
}
