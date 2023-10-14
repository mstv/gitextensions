using System.Diagnostics;

namespace GitExtUtils
{
    /// <summary>
    ///  Set of DEBUG-only helpers.
    /// </summary>
    public static class DebugHelpers
    {
        [Conditional("DEBUG")]
        public static void Assert(bool condition, string message)
        {
            if (Debugger.IsAttached)
            {
                Debug.Assert(condition, message);
            }
            else
            {
                Debugger.Launch();
            }
        }

        [Conditional("DEBUG")]
        public static void Fail(string message)
        {
            if (Debugger.IsAttached)
            {
                Debug.Fail(message);
            }
            else
            {
                Debugger.Launch();
            }
        }
    }
}
