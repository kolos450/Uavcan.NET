using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Uavcan.NET.Testing.Framework
{
    public static class IOUtils
    {
        public static void DirectoryCopy(string sourceDirName, string destDirName,
                                      bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                WithRetries(() =>
                {
                    try
                    {
                        file.CopyTo(temppath, false);
                    }
                    catch (DirectoryNotFoundException) { }
                }
                );
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

        public static string GetTemporaryDirectory()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        public static void WithRetries(Action action)
        {
            int retries = 0;
        restart:
            try
            {
                action();
            }
            catch (Exception ex) when (retries < 10 && ex is IOException || ex is UnauthorizedAccessException)
            {
                Thread.Sleep(100);
                retries++;
                goto restart;
            }
        }

        public static void DirectoryDelete(string path)
        {
            WithRetries(() =>
            {
                var files = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories);
                foreach (var f in files)
                {
                    File.Delete(f);
                }
                Directory.Delete(path, true);
            });
        }
    }
}
