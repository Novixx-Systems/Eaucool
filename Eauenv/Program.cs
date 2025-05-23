﻿using System.Diagnostics;
using System.IO.Compression;
using System.Net;

namespace Eauenv
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: eauenv <environment name>");
                Console.WriteLine("If the environment does not exist, it will be created.");
                return;
            }
            // Get the environment name
            string envName = args[0];

            // Check if userprofile/.eauenv exists
            if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".eauenv")))
            {
                Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".eauenv"));
            }

            // Check if the environment exists
            if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".eauenv", envName)))
            {
                Console.WriteLine("Creating environment " + envName);
                // Create the environment
                Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".eauenv", envName));
                // Create the shell
#if WINDOWS
                File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".eauenv", envName, "shell.bat"), "@echo off\nset PATH=%PATH%;" + Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".eauenv", envName) + "\n");
                File.AppendAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".eauenv", envName, "shell.bat"), "set PATH=%PATH%;" + Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".eauenv", envName) + "\\bin\n");
#else
                File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".eauenv", envName, "shell.sh"), "export PATH=$PATH:" + Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".eauenv", envName) + "\n");
                File.AppendAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".eauenv", envName, "shell.sh"), "export PATH=$PATH:" + Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".eauenv", envName) + "/bin\n");
#endif
                Console.WriteLine("Downloading latest Eaucool release...");
                // Download latest Eaucool release
                WebClient client = new WebClient();
#if WINDOWS
                client.DownloadFile("https://www.github.com/Novixx-Systems/eaucool/releases/latest/download/windows.zip", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".eauenv", envName, "eaucool.zip"));
#else
                client.DownloadFile("https://www.github.com/Novixx-Systems/eaucool/releases/latest/download/linux.zip", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".eauenv", envName, "eaucool.zip"));
#endif
                Console.WriteLine("Extracting Eaucool...");
                // Extract Eaucool
                ZipFile.ExtractToDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".eauenv", envName, "eaucool.zip"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".eauenv", envName));
                // Delete the zip
                File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".eauenv", envName, "eaucool.zip"));
                // Done
                Console.WriteLine("Created environment " + envName);
            }
            Console.Clear();
            Console.WriteLine("Starting environment " + envName);
            // Start shell
            ProcessStartInfo startInfo = new ProcessStartInfo();
            #if WINDOWS
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/K " + Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".eauenv", envName, "shell");
#else
            startInfo.FileName = "bash";
            startInfo.Arguments = "-c \"source " + Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".eauenv", envName, "shell.sh") + " && exec bash\"";
#endif
            startInfo.UseShellExecute = true;
            // Make it a window
            startInfo.WindowStyle = ProcessWindowStyle.Normal;
            // Start the process
            Process.Start(startInfo);
        }
    }
}