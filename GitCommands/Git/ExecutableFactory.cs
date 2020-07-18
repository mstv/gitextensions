using System;
using System.Windows.Forms;
using GitUIPluginInterfaces;
using JetBrains.Annotations;

namespace GitCommands.Git
{
    public class ExecutableFactory
    {
        /// <summary>
        /// Singleton accessor.
        /// </summary>
        public static ExecutableFactory Default { get; } = new ExecutableFactory();

        public IExecutable Create([NotNull] Func<string> fileNameProvider, [NotNull] string workingDir = "", bool notifyOnException = true)
            => new Executable(fileNameProvider, workingDir, notifyOnException);

        public IExecutable Create([NotNull] string fileName, [NotNull] string workingDir = "", bool notifyOnException = true)
            => Create(() => fileName, workingDir, notifyOnException);

        public IProcess Spawn(string arguments, [NotNull] string workingDir = "", bool notifyOnException = true)
            => Create(Application.ExecutablePath, workingDir, notifyOnException).Start(arguments);
    }
}
