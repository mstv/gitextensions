using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace CommonTestUtils
{
    public sealed class TempFileDeleter : IDisposable
    {
        private List<string> _tempFileNames = new();

        public void Add(string tempFileName)
        {
            _tempFileNames.Add(tempFileName);
        }

        public void DeleteAll()
        {
            _tempFileNames.ForEach(name =>
            {
                try
                {
                    if (File.Exists(name))
                    {
                        File.Delete(name);
                        ////Debug.Write($"{nameof(TempFileDeleter)} deleted the file {name}\n");
                    }
                    else
                    {
                        ////Debug.Write($"{nameof(TempFileDeleter)}: non-existing file {name}\n");
                    }
                }
                catch (Exception ex)
                {
                    Debug.Write(ex);
                }
            });
            _tempFileNames.Clear();
        }

        public void Dispose()
        {
            DeleteAll();
        }

        public string GetTempFileName()
        {
            string tempFileName = Path.GetTempFileName();
            _tempFileNames.Add(tempFileName);
            return tempFileName;
        }
    }
}
