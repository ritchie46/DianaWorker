using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Windows.Interop;
using WindowsInput.Native;

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


            // Read solver.bat from resources
            Stream stream = null;
            switch (version)
            {
                case "10.1":
                    stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ServerWorker.Resources.solver10.1.bat");
                    break;
                case "10.0":
                    stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ServerWorker.Resources.solver10.0.bat");
                    break;
                case "EV.0":
                    stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ServerWorker.Resources.solverDEV.0.bat");
                    version = "DEVELOPMENT 10.0";
                    break;
            }

            using (TextReader tr = new StreamReader(stream))
            {
                var solver_file = tr.ReadToEnd();

                // Append information to solver.bat
                title = Path.GetFileName(path);
                title = title.Remove(title.Length - 4);

                solver_file += $"\r\ncd {root}\r\ntitle Diana {version} Command Box - PROJECT: {title}" +
                               "\r\necho starting calculation in:\ntimeout 5";
                solver_file += $"\r\n    diana -m {title} {title}.ff";

                using (var solv_f = new StreamWriter(Path.Combine(root, "solver.bat")))
                {
                    solv_f.Write(solver_file);
                    solv_f.Close();

                    var filename = Path.Combine(root, "solver.bat");
                    
                    // Start python script
                    DianaLive.Start(root, title);

                    // Start convergence checker
                    //Task.Run(() => ConvergenceChecker.check());

                    await Task.Run(() =>
                    {
                        RunProcessAsync(filename);
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
                        File.Delete(Path.Combine(root, "solver.bat"));
                        File.Delete(Path.Combine(root, "parser.pyw"));
                    }
                    catch (Exception)
                    {
                        Debug.WriteLine("Exception occurred");
                    }
                    return $"Finished task : {Path.Combine(root, title)} at {DateTime.Now.ToShortTimeString()}";
                }
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private static Task RunProcessAsync(string fileName)
        // Async method for waiting for the commandbox to be finished.
        {

            // there is no non-generic TaskCompletionSource
            var tcs = new TaskCompletionSource<bool>();

            var process = new Process
            {
                StartInfo = { FileName = fileName },

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
                        process.Start();
                        processRunning = true;
                    }
                    if (MainWindow.cancelNowRunning)
                    {
                        try
                        {
                            IntPtr pointer = process.MainWindowHandle;
                            AsyncDia.SetForegroundWindow(pointer);
                            System.Threading.Thread.Sleep(200);

                            var sim = new WindowsInput.InputSimulator();
                            sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_C);
                            System.Threading.Thread.Sleep(200);
                            sim.Keyboard.KeyPress(VirtualKeyCode.VK_Y);
                            sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
                            MainWindow.cancelNowRunning = false;
                            break;
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                ((MainWindow) System.Windows.Application.Current.MainWindow).output(ex.Message);
                            }

                            catch (Exception exp)
                            {
                                Debug.WriteLine(exp.Message);
                            }
                            MainWindow.cancelNowRunning = false;
                            System.Threading.Thread.Sleep(900000); // Sleep 15 minutes
          
                        }
                    }

                    if (process.HasExited)
                    {
                        break;
                    }
                }
            }
            catch (InvalidOperationException)
            {
                Debug.WriteLine("Console interrupted");
            }
            ConvergenceChecker.stopMe = true;
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
        var pythonstream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ServerWorker.Resources.parser.pyw");
        var path = Path.Combine(root, "parser.pyw");

        using (var f = new StreamReader(pythonstream))
        {

            var script = f.ReadToEnd();

            using (var fw = new StreamWriter(path))
            {
                fw.Write(script);
            }
        }

        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.FileName = "cmd.exe";
        startInfo.Arguments = "C:/Anaconda3/python.exe"; 
        DianaLive.p = Process.Start("C:/Anaconda3/pythonw", $"{path} {root} {path}/{title}.out");
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
