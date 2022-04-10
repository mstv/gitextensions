using System.Diagnostics;
using System.Runtime.InteropServices;
using GitCommands.Utils;

namespace GitCommands.Git.Extensions
{
    public static class ProcessExtensions
    {
        public static bool SendTerminateRequest(this Process process)
        {
            if (EnvUtils.RunningOnWindows())
            {
                // Send Ctrl+C
                if (!NativeMethods.AttachConsole(process.Id))
                {
                    return false;
                }

                _ = NativeMethods.SetConsoleCtrlHandler(IntPtr.Zero, add: true);

                bool result = NativeMethods.GenerateConsoleCtrlEvent(0, 0);

                _ = NativeMethods.FreeConsole();

                return result;
            }

            return false;
        }

        public static void TerminateTree(this Process process)
        {
            if (process.SendTerminateRequest())
            {
                if (process.HasExited)
                {
                    return;
                }

                process.WaitForExit(500);
            }

            if (!process.HasExited)
            {
                process.Kill();
            }
        }

        private static class NativeMethods
        {
            [DllImport("kernel32.dll")]
            public static extern bool SetConsoleCtrlHandler(IntPtr handlerRoutine, bool add);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool AttachConsole(int dwProcessId);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool FreeConsole();

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, int dwProcessGroupId);
        }
    }
}
