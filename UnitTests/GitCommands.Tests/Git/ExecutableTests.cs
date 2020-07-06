using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CommonTestUtils;
using FluentAssertions;
using GitCommands;
using GitCommands.Git;
using GitCommands.Settings;
using GitExtUtils;
using GitUIPluginInterfaces;
using NUnit.Framework;

namespace GitCommandsTests.Git
{
    [TestFixture]
    public sealed class ExecutableTests
    {
        private const string _invalidExe = "invalid.exe";

        private ExecutableException _executableException;

        private void RecordExecutableException(ExternalOperationException ex)
            => _executableException = ex as ExecutableException;

        [SetUp]
        public void SetUp()
        {
            _executableException = null;
            ExternalOperationExceptionFactory.Default.OnException += RecordExecutableException;
        }

        [TearDown]
        public void TearDown()
        {
            ExternalOperationExceptionFactory.Default.OnException -= RecordExecutableException;
        }

        [Test]
        public void StartNonexisting()
        {
            IExecutable executable = new Executable(_invalidExe);

            ExecutableException ex = Assert.Throws<ExecutableException>(() => executable.Start());

            ex.ProcessStartInfo.FileName.Should().Be(_invalidExe);
            ex.Should().BeSameAs(_executableException);
        }

        [Test]
        public void GetOutputNonexisting()
        {
            IExecutable executable = new Executable(_invalidExe);

            ExecutableException ex = Assert.Throws<ExecutableException>(() => executable.GetOutput(""));

            ex.ProcessStartInfo.FileName.Should().Be(_invalidExe);
            ex.Should().BeSameAs(_executableException);
        }

        [Test]
        public void StartExistingInInvalidDir()
        {
            string executableName = "cmd.exe";
            string workingDirectory = @"C:\nonexistent-dir";
            IExecutable executable = new Executable(executableName, workingDirectory);

            ExecutableException ex = Assert.Throws<ExecutableException>(() => executable.Start());

            ex.ProcessStartInfo.FileName.Should().Be(executableName);
            ex.ProcessStartInfo.WorkingDirectory.Should().Be(workingDirectory);
            ex.Should().BeSameAs(_executableException);
        }
    }
}
