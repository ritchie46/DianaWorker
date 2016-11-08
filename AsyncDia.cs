using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Diagnostics;
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
                        content = "*FILOS" + Environment.NewLine + "  INITIA" + Environment.NewLine + " * INPUT" + Environment.NewLine + content;
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

                solver_file += String.Format("\r\ncd {0}\r\ntitle Diana {1} Command Box - PROJECT: {2}" +
                                             "\r\necho starting calculation in:\ntimeout 5", root, version, title);
                solver_file += String.Format("\r\n    diana -m {0} {1}.ff", title, title);

                using (var solv_f = new StreamWriter(Path.Combine(root, "solver.bat")))
                {
                    solv_f.Write(solver_file);
                    solv_f.Close();

                    var filename = Path.Combine(root, "solver.bat");

                    Task.Run(() => ConvergenceChecker.check());
                    await Task.Run(() =>
                    {
                        RunProcessAsync(filename);
                    });

                    // Remove Filos File
                    var dir = new DirectoryInfo(root);
                    var ff_files = dir.GetFiles("*.ff");

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
                    }
                    catch (Exception)
                    {
                        Debug.WriteLine("Exception occurred");
                    }
                    return String.Format("Finished task : {0} at {1}",
                        Path.Combine(root, title),
                        DateTime.Now.ToShortTimeString());
                }
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        static Task RunProcessAsync(string fileName)
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
                    if (AsyncDia.stop_convergence)
                    {

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
                Mail.sendMail(mailAdress);
            }
            return tcs.Task;
        }
    }
}
