using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Net.Mime;
using System.Windows;
using System.Windows.Forms.PropertyGridInternal;
using System.Windows.Interop;
using WindowsInput.Native;
using ServerWorker.Properties;

namespace ServerWorker
{
    public class AsyncDia
    {
        // A list with paths to .dat files
        public static List<string> jobs = new List<string>();
        public static List<string> diana_version = new List<string>();
        public static List<bool> stop_conv = new List<bool>();
        public static List<double> conv_val = new List<double>();
        public static List<string> email = new List<string>();
        public static string root = null;
        public static string title = null;
        public static int count = 0;
        public static bool stop_convergence = false;
        public static double convergence_value = 1;

        public static async Task<string> add_job_(string path, string version, bool stopConv, double value)
        {
            // Directory root
            root = Directory.GetParent(path).ToString();
            stop_convergence = stopConv;
            convergence_value = value;

            title = Path.GetFileName(path);
            title = title.Remove(title.Length - 4);

            var dcf = path.Remove(path.Length - 4) + ".dcf";
            // Check if filos in instantiated
            string content;

            try
            {
                using (FileStream str = File.Open(dcf, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    using (StreamReader rd = new StreamReader(str))
                    {
                        content = rd.ReadToEnd();
                    }
                }

                if (!(content.Contains("*FILOS") && content.Contains("*INPUT")))
                {
                    File.Delete(dcf);
                    using (StreamWriter rw = new StreamWriter(dcf, false))
                    {
                        content = "*FILOS" + Environment.NewLine + "  INITIA" + Environment.NewLine + "*INPUT" + Environment.NewLine + content;
                        rw.Write(content);
                    }
                }
            }
            catch (FileNotFoundException)
            {
                try
                {
                    ((MainWindow)System.Windows.Application.Current.MainWindow).output("Could not find a .dcf file.");
                }
                catch (Exception)
                {
                    Debug.WriteLine("Could not find a .dcf file.");
                }
                MainWindow.cancelNowRunning = true;
            }
                    
                // Start python script
                DianaLive.Start(root, title);

                await Task.Run(() =>
                {
                    RunProcessAsync(root, title, version);
                });

                // Remove Filos File
                var dir = new DirectoryInfo(root);
                var ff_files = dir.GetFiles("*.ff");

                // Stop python script
                DianaLive.Stop();

                foreach (FileInfo filos in ff_files)
                {
                    try
                    {
                        filos.Attributes = FileAttributes.Normal;
                        File.Delete(filos.FullName);
                    }
                    catch (Exception)
                    {
                        Debug.WriteLine("Exception occurred");
                    }
                }
                try
                {
                    File.Delete(Path.Combine(root, "parser.pyw"));
                    File.Delete(Path.Combine(root, ".007.py"));
                }
                catch (Exception)
                {
                    Debug.WriteLine("Exception occurred");
                }

                return $"Finished task : {Path.Combine(root, title)} at {DateTime.Now.ToShortTimeString()}";
            }
          


        private static Task RunProcessAsync(string root, string title, string version)
        // Async method for waiting for the commandbox to be finished.
        {

            // Write the solver file to the root directory
            var path = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Resources", "007.py");
            string script;
            using (var sr = new StreamReader(path))
            {
                script = sr.ReadToEnd();
            }
            path = Path.Combine(root, ".007.py");

            using (var fw = new StreamWriter(path))
            {
                fw.Write(script);
            }

            // there is no non-generic TaskCompletionSource
            var tcs = new TaskCompletionSource<bool>();

            //var process = new Process();
            var process = new Process 
            {
                StartInfo =
                {
                    FileName = ServerWorker.Settings.PythonPath,
                    Arguments = $"{path} {title} {version}"
                },

            };

            process.Exited += (sender, args) =>
            {
                tcs.SetResult(true);
                process.Dispose();
            };

            var processRunning = false;
            try
            {
                while (true)
                {
                    if (!processRunning)
                    {   
                        Debug.WriteLine("Start new process");
                        process.Start();
                        processRunning = true;
                    }

                    if (process.HasExited)
                    {   
                        Debug.WriteLine("Process has exited");
                        break;
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(3000);
                    }
                }
            }
            catch (InvalidOperationException)
            {
                Debug.WriteLine("Console interrupted");
            }

            var mailAdress = AsyncDia.email[AsyncDia.count];
            if (mailAdress != "none")
            {
                Mail.sendMail(mailAdress, "Your DIANA calculation is finished.", "This is an automatically generated e-mail.");
            }
            return tcs.Task;
        }
    }
}

internal class DianaLive
{   
    private static Process p = new Process();

    public static void Start(string root, string title)
    {
 
        var dir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        var path = Path.Combine(dir, "Resources", "parser.py");

        string script;
        using (var sr = new StreamReader(path))
        {
            script = sr.ReadToEnd();
        }
        path = Path.Combine(root, "parser.pyw");

        using (var fw = new StreamWriter(path))
        {
            fw.Write(script);
        }

        DianaLive.p = Process.Start(ServerWorker.Settings.PythonwPath, $"{path} {root} {root}\\{title}.out");
    }

    public static void Stop()
    {
        try
        {
            DianaLive.p.Kill();
        }
        catch
        {
            
        }
    }
}

