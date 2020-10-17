using System;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;

namespace PersistentChangeLogCreator
{
    class Program
    {

        const string git = "git";
        const string gitInit = "init .";
        const string gitStatus = "status";
        const string gitAdd = "add persistent.sfs";
        const string gitCommit = "commit -m \"updated\"";

        static string gitWd = string.Empty;

        static void Main(string[] args)
        {
            Console.WriteLine(string.Join(',', args));
            if (args.Length != 1)
            {
                // Display the proper way to call the program.
                Console.WriteLine("Usage: PersistentChangeLogCreator.exe (directory)");
                return;
            }

            using (FileSystemWatcher watcher = new FileSystemWatcher())
            {
                watcher.Path = args[0];
                watcher.NotifyFilter = NotifyFilters.LastWrite;
                watcher.Filter = "persistent.sfs";

                var persistentSfsPath = Path.Combine(watcher.Path, watcher.Filter);
                if (!File.Exists(persistentSfsPath))
                {
                    Console.WriteLine($"Directory is invalid, did not find a {persistentSfsPath} file.");
                    Environment.Exit(1);
                }

                gitWd = watcher.Path;

                watcher.Changed += OnChanged;
                watcher.Created += OnChanged;
                watcher.Deleted += OnChanged;

                watcher.EnableRaisingEvents = true;

                Console.WriteLine("Press 'q' and enter to quit.");
                while (Console.Read() != 'q') ;
            }
        }


        private static Process Git(string cmd, bool ignoreOutput = true)
        {
            var psi = new ProcessStartInfo
            {
                FileName = git,
                Arguments = cmd,
                UseShellExecute = false,
                RedirectStandardOutput = !ignoreOutput,
                RedirectStandardError = !ignoreOutput
            };
                
            psi.WorkingDirectory = gitWd;
            var process = Process.Start(psi);
            process.WaitForExit();
            return process;
        }

        private static void EnsureIsGitRepository()
        {
            if (Git(gitStatus, false).ExitCode == 0)
            {
                return;
            }

            if (Git(gitInit).ExitCode == 0)
            {
                return;
            }

            Console.WriteLine("Terminal failure, can't create repository");
            Environment.Exit(1);
        }

        private static void AddChange()
        {
            if (Git(gitAdd).ExitCode != 0)
            {
                Console.WriteLine("Failed to add to index");
                return;
            }

            if (Git(gitCommit).ExitCode != 0)
            {
                Console.WriteLine("Failed to commit");
                return;
            }
        }

        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            Console.WriteLine($"File: {e.FullPath} {e.ChangeType}");
            EnsureIsGitRepository();
            AddChange();
        }
    }
}
