using System;
using System.Diagnostics;
using System.IO;
using JetBrains.Annotations;

namespace GitCommands.Git
{
    public class ExecutableException : ExternalOperationException
    {
        public ExecutableException(ProcessStartInfo processStartInfo, [NotNull] Exception inner)
            : base(inner)
        {
            ProcessStartInfo = processStartInfo;
        }

        /// <summary>
        /// Gets the start info of the executable which could not be started.
        /// </summary>
        public ProcessStartInfo ProcessStartInfo { get; }
    }
}
