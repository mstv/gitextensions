using System;

namespace GitExtUtils
{
    /// <summary>
    /// Represents errors that occur during execution of an external operation,
    /// e.g. running a git operation or launching an external process.
    /// </summary>
    public class ExternalOperationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExternalOperationException"/> class with a specified parameters
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        /// <param name="command">The command that led to the exception.</param>
        /// <param name="arguments">The command arguments.</param>
        /// <param name="directory">The directory of the operation, e.g. the working directory or the git repo.</param>
        /// <param name="exitCode">The exit code of an executed process.</param>
        public ExternalOperationException(
            Exception? innerException = null,
            string? command = null,
            string? arguments = null,
            string? directory = null,
            int? exitCode = null)
            : base(innerException?.Message, innerException)
        {
            Command = command;
            Arguments = arguments;
            Directory = directory;
            ExitCode = exitCode;
        }

        /// <summary>
        /// The command that led to the exception.
        /// </summary>
        public string? Command { get; }

        /// <summary>
        /// The command arguments.
        /// </summary>
        public string? Arguments { get; }

        /// <summary>
        /// The directory of the operation, e.g. the working directory or the git repo.
        /// </summary>
        public string? Directory { get; }

        /// <summary>
        /// The exit code of an executed process.
        /// </summary>
        public int? ExitCode { get; }
    }
}
