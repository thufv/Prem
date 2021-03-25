using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main()
        {
        }

        public bool RecursiveCopy()
        {
            string origDir = @"D:\Documents and Settings\Dub\My Documents\HoN Updates\test";
            string destDir = @"C:\Games\HoN";
            bool status = false;
            //get all the info about the original directory
            DirectoryInfo dirInfo = new DirectoryInfo(origDir);
            //retrieve all the _fileNames in the original directory
            FileInfo[] files = dirInfo.GetFiles(origDir);
            //always use a try...catch to deal 
            //with any exceptions that may occur
            try
            {
                //loop through all the file names and copy them
                foreach (string file in Directory.GetFiles(origDir))
                {
                    FileInfo origFile = new FileInfo(file);
                    FileInfo destFile = new FileInfo(file.Replace(origDir, destDir));
                    //copy the file, use the OverWrite overload to overwrite
                    //destination file if it exists
                    System.IO.File.Copy(origFile.FullName, destFile.FullName, true);
                    //TODO: If you dont want to remove the original
                    //_fileNames comment this line out
                    File.Delete(origFile.FullName);
                    status = true;
                }
                Console.WriteLine("All files in " + origDir + " copied successfully!");
            }
            catch (Exception ex)
            {
                status = false;
                //handle any errors that may have occurred
                Console.WriteLine(ex.Message);
            }
            return status;
        }

        public string origDir = @"D:\Documents and Settings\Dub\My Documents\HoN Updates\test"; // ERROR HERE
        public string destDir = @"C:\Games\HoN"; // ERROR HERE

        static void RecursiveCopy(origDir)
        {
            Console.WriteLine("done");
            Console.ReadLine();
        }
    }
}