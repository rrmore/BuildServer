////////////////////////////////////////////////////////////////////////////////////////
// BuildLibrary.cs : This module build libraries for cs fileList                      //
// ver 1.0                                                                            //
//                                                                                    //
//Language:     Visual C#                                                             //
// Platform    : Acer Aspire R, Win Pro 10, Visual Studio 2017                        //
// Application : CSE-681 SMA Project 4                                                //
// Author      : Rohit More,Syracuse University                                       //
// Source      : Dr. Jim Fawcett, EECS, SU                                            //
////////////////////////////////////////////////////////////////////////////////////////

/*
* Module Operations:
*===================
* This mule performs buillding of libraris
*
* public Interfaces:
* =================
* libraryBuilder(): spawns a process to build a dll fileName from cs files.
*
* Required Files:
* ===============
* TestInterfaces.dll
*
* Maintainance History:
* =====================
* ver 1.0
*
*/

using System;
using System.Collections.Generic;
using System.IO;


namespace Project4
{
    public class Builder
    {
        //---------------------------builds librarys using the specified directory and file list-----------------------------------------
        public string libraryBuilder(List<string> files, String filePath)
        {
            Console.Write("\n ----------------------Building Library------------------------------------------");
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            String fileList = "";
            foreach (string fileName in files)
            {
                string filename = Path.GetFileName(fileName);
                fileList = fileList + " " + filename;
            }
            fileList = "/nologo /target:library  /r:../../TestInterfaces/bin/Debug/TestInterfaces.dll" + fileList;
            Console.Write("\n Build Command:" + "csc " + fileList);
            process.StartInfo.FileName = "csc";
            process.StartInfo.Arguments = fileList;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.WorkingDirectory = filePath;
            process.StartInfo.RedirectStandardOutput = true;
            //Start the process
            process.Start();
            //Get program output
            string output = process.StandardOutput.ReadToEnd();
            if (output == "") Console.Write("\n Build Success");
            else
            {
                Console.Write("\n Build Status: Build Failed");
                Console.WriteLine("\n Failure message:" + output);
            }
            //-----------------Wait for process to finish----------------------------------------------------------
            process.WaitForExit();
            return output;
        }

#if (DEMO)
        static void Main(string[] args)
        {
            string Storagepath = @"../../../RepoStorage";
            Console.Write("Repository path is:"+Storagepath);
            Builder build = new Builder();
            build.libraryBuilder(repo.files,Storagepath);
            Console.Read();
        }
#endif
    }
}
