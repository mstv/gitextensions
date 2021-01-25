using System;
using System.IO;

namespace GitExtUtils
{
    public static class FileSystemUtility
    {
        /// <summary>
        /// Returns the current working directory for display purposes or the exception message in the unlikely case of inability.
        /// </summary>
        public static string GetWorkingDirectoryNoEx()
        {
            try
            {
                return Directory.GetCurrentDirectory();
            }
            catch (Exception exception)
            {
                return $"{exception.GetType().FullName}: {exception.Message}";
            }
        }
    }
}
