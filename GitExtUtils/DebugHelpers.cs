using System.Diagnostics;

namespace GitExtUtils
{
    public static class DebugHelpers
    {
        [Conditional("DEBUG")]
        public static void Fail(string message)
        {
            if (Debugger.IsAttached)
            {
                Debug.Fail(message);
            }
            else
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
