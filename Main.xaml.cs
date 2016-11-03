using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace ServerWorker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool jobsRunning = false;
        private bool pathChosen = false; 
        private List<string> queue = new List<string>();
        public static bool cancelNowRunning = false;
        
        public MainWindow()
        {
            InitializeComponent();
        }


        private void addPath(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".dat",
                Filter = "*.dat | *.DAT"
            };
            var succes = dialog.ShowDialog();
   
            if (succes == true)
            {
                pathChosen = true;

                // Set the value to the path_dat label
                path_dat.Content = dialog.FileName;
            }
        }

        private async void addJob(object sender, RoutedEventArgs e)
            // Add job to task queue
        {
            if (!pathChosen)
            {
                MessageBox.Show("The path to your .dat file is not given");
            }
            else
            {
                var version = diana_version.SelectedItem.ToString().Substring(diana_version.SelectedItem.ToString().Length - 4);

                // Add job to queue. This makes sure the correct jobs are printed.
                queue.Add(String.Format("{0} [{1}]", path_dat.Content.ToString(), version));
                refreshQueueTextbox();

                AsyncDia.jobs.Add(path_dat.Content.ToString());
                AsyncDia.diana_version.Add(version);

                if (!jobsRunning)
                {   
                    // reset job list
                    AsyncDia.jobs = new List<string> { };
                    AsyncDia.jobs.Add(path_dat.Content.ToString());
                    AsyncDia.diana_version = new List<string> { };
                    AsyncDia.diana_version.Add(version);

                    addToOutputbox("Starting first job.\r\n");
                    jobsRunning = true;
                    await firstCall();
                    addToOutputbox("All jobs finished");
                    jobsRunning = false;
                }
            }
        }

        private void removeLast(object sender, RoutedEventArgs e)
            // Remove last task from the queue
        {
            if ((queue.Count > 0 && !jobsRunning) || queue.Count > 1)
            {
                queue.RemoveAt(queue.Count - 1);
                AsyncDia.jobs.RemoveAt(AsyncDia.jobs.Count - 1);
                AsyncDia.diana_version.RemoveAt(AsyncDia.diana_version.Count - 1);
                refreshQueueTextbox();      
            }
        }

        private void insertJob(object sender, RoutedEventArgs e)
        {
            if (!jobsRunning)
            {
                MessageBox.Show("There are no jobs running. \r\n\nAdd a job via the 'add job' button.");
            }
            else
            {
                var input = new rowIndexDialog();
                if (input.ShowDialog() == true)
                {
                    var insert = input.queueIndex;

                    if (insert > 0)  // the input is correct
                    {
                        if (insert + AsyncDia.count >= AsyncDia.jobs.Count)
                        {
                            addJob(sender, e);
                        }
                        else
                        {
                            var version = diana_version.SelectedItem.ToString().Substring(diana_version.SelectedItem.ToString().Length - 4);

                            // Add job to queue. This makes sure the correct jobs are printed.
                            queue.Insert(insert, String.Format("{0}[{1}]", path_dat.Content.ToString(), version));
                            refreshQueueTextbox();

                            AsyncDia.jobs.Insert(insert + AsyncDia.count , path_dat.Content.ToString());
                            AsyncDia.diana_version.Insert(insert + AsyncDia.count , version);

                        }
                    }
                }                
            }
        }

        private void removeJob(object sender, RoutedEventArgs e)
        {
            if (!jobsRunning)
            {
                MessageBox.Show("There are no jobs running. \r\n\nAdd a job via the 'add job' button.");
            }
            else
            {
                var input = new rowIndexDialog
                {
                    Title = "Remove a job"
                };
                input.queueIndexLabel.Content = "Give the index of the job you want to remove.";
                if (input.ShowDialog() == true)
                {
                    if(input.queueIndex > 0) // The input is correct
                    {
                        if(input.queueIndex + AsyncDia.count >= AsyncDia.jobs.Count)
                        {
                            MessageBox.Show("There index is larger than the number of jobs in the queue.\r\n Please use another index.");
                        }
                        else
                        {
                            queue.RemoveAt(input.queueIndex);
                            refreshQueueTextbox();
                            AsyncDia.jobs.RemoveAt(input.queueIndex);
                            AsyncDia.diana_version.RemoveAt(input.queueIndex);
                        }
                    }
                }
            }
        }

        private void detachJob(object sender, RoutedEventArgs e)
        {
            if (jobsRunning)
            {
                //cts.Cancel();
                MainWindow.cancelNowRunning = true;
            }
        }

        private async Task firstCall()
            // Starts working on the DIANA task queue.
        {
            var count = 0;
            while (count < AsyncDia.jobs.Count)
            {
                AsyncDia.count = count;
                addToOutputbox(String.Format("Starting {0} [{1}] at {2}", 
                    queue[0],
                    AsyncDia.diana_version[count],
                    DateTime.Now.ToShortTimeString()));

                string outp = null;
                                
                // Make sure the job will get a license and will not be skipped.   
                while (true)
                {
                    outp = await AsyncDia.add_job_(AsyncDia.jobs[count], AsyncDia.diana_version[count]);

                    // wait 2 seconds, to make sure .out file is created.
                    System.Threading.Thread.Sleep(2000);

                    var dir = new DirectoryInfo(AsyncDia.root);
                    var outFile = dir.GetFiles(String.Format("{0}.out", AsyncDia.title));

                    try
                    {
                        using (StreamReader sr = new StreamReader(outFile[0].FullName))
                        {
                            var content = sr.ReadToEnd();
                            if (!content.Contains("All licensed seats"))
                            {
                                break;
                            }
                        }
                    }
                    catch (IndexOutOfRangeException) // If there is not .out file. Break
                    {
                        break;
                    }
                }

                addToOutputbox(outp);

                // remove path from tasklist
                queue.RemoveAt(0);
                refreshQueueTextbox();
                count++;
            }
        }

        private void refreshQueueTextbox()
            // Refreshes the job list/ task queue textbox
        {
            // Refresh job list
            queueTextbox.Document.Blocks.Clear();
            for (int i = 0; i < queue.Count; i++)
            {
                if (i == 0)
                {
                    queueTextbox.AppendText(String.Format("[Now Running] {0}\r\n\n", queue[i]));
                }
                else
                {
                    queueTextbox.AppendText(String.Format("[index {0}] {1}\r\n", i, queue[i]));
                }
                queueTextbox.ScrollToEnd();
            }
        }

        private void addToOutputbox(string output)
        {
            outputBox.AppendText(output + "\r\n");
            outputBox.ScrollToEnd();
        }
    }
}


public class AsyncDia
{
    // A list with paths to .dat files
    public static List<string> jobs = new List<string>();
    public static List<string> diana_version = new List<string>();
    public static string root = null;
    public static string title = null;
    public static int count = 0;
    
    public static async Task<string> add_job_(string path, string version)
    {
        // Directory root
        root = Directory.GetParent(path).ToString();

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
            title = System.IO.Path.GetFileName(path);
            title = title.Remove(title.Length - 4);

            solver_file += String.Format("\r\ncd {0}\r\ntitle Diana {1} Command Box - PROJECT: {2}" +
                                         "\r\necho starting calculation in:\ntimeout 5", root, version, title);
            solver_file += String.Format("\r\n    diana -m {0} {1}.ff", title, title);

            using (var solv_f = new StreamWriter(System.IO.Path.Combine(root, "solver.bat")))
            {
                solv_f.Write(solver_file);
                solv_f.Close();

                var filename = System.IO.Path.Combine(root, "solver.bat");

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
                    File.Delete(System.IO.Path.Combine(root, "solver.bat"));
                }
                catch (Exception)
                {
                    Debug.WriteLine("Exception occurred");
                }
                return String.Format("Finished task : {0} at {1}",
                    System.IO.Path.Combine(root, title),
                    DateTime.Now.ToShortTimeString());
            }
        }
    }

    static  Task RunProcessAsync(string fileName)
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
                if (ServerWorker.MainWindow.cancelNowRunning)
                {
                    process.Kill();
                    process.Dispose();
                    ServerWorker.MainWindow.cancelNowRunning = false;
                    return tcs.Task;
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
        return tcs.Task;
    }
}
