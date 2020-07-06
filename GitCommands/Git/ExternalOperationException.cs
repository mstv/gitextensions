using System;
using JetBrains.Annotations;

namespace GitCommands.Git
{
    /// <summary>
    /// Base class for failures of external operations.
    /// External operations, e.g. file access or start of other executables, always can fail due to removed or locked files.
    /// </summary>
    public class ExternalOperationException : Exception
    {
        public ExternalOperationException([NotNull] Exception inner)
            : base(inner.Message, inner)
        {
        }

        /// <summary>
        /// Flag whether this exception has already been handled, e.g. shown to the user,
        /// and shall not be reported as a bug.
        /// </summary>
        public bool AbortSilently { get; set; } = false;
    }
}
