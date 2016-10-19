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
        private bool jobs_running = false;
        private bool path_chosen = false; 
        private List<string> queue = new List<string> { };
        
        public MainWindow()
        {
            InitializeComponent();
        }


        private void add_path(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.DefaultExt = ".dat";
            dialog.Filter = "*.dat | *.DAT";
            var succes = dialog.ShowDialog();
   
            if (succes == true)
            {
                path_chosen = true;

                // Set the value to the path_dat label
                path_dat.Content = dialog.FileName;
            }
        }

        private async void add_job(object sender, RoutedEventArgs e)
            // Add job to task queue
        {
            if (!path_chosen)
            {
                MessageBox.Show("The path to your .dat file is not given");
            }
            else
            {
                string version = diana_version.SelectedItem.ToString().Substring(diana_version.SelectedItem.ToString().Length - 4);

                // Add job to queue. This makes sure the correct jobs are printed.
                queue.Add(String.Format("\r\n{0}[{1}]", path_dat.Content.ToString(), version));
                refreshQueueTextbox();

                AsyncDia.jobs.Add(path_dat);
                AsyncDia.diana_version.Add(version);

                if (!jobs_running)
                {   
                    // reset job list
                    AsyncDia.jobs = new List<Label> { };
                    AsyncDia.jobs.Add(path_dat);
                    AsyncDia.diana_version = new List<string> { };
                    AsyncDia.diana_version.Add(version);

                    addToOutputbox("Starting first job.");
                    jobs_running = true;
                    await first_call();
                    addToOutputbox("All jobs finished");
                    jobs_running = false;
                }
            }
        }

        private void remove_last(object sender, RoutedEventArgs e)
            // Remove last task from the queue
        {
            if ((queue.Count > 0 && !jobs_running) || queue.Count > 1)
            {
                queue.RemoveAt(queue.Count - 1);
                AsyncDia.jobs.RemoveAt(AsyncDia.jobs.Count - 1);
                AsyncDia.diana_version.RemoveAt(AsyncDia.diana_version.Count - 1);
                refreshQueueTextbox();      
            }
        }

        private async Task first_call()
            // Starts working on the DIANA task queue.
        {
            int count = 0;
            while (count < AsyncDia.jobs.Count)
            {
                addToOutputbox(String.Format("Starting {0} [{1}]", path_dat.Content.ToString(),
                    AsyncDia.diana_version[count]));
                string outp = await AsyncDia.add_job_(AsyncDia.jobs[count], AsyncDia.diana_version[count]);
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
            foreach (string path in queue)
            {
                queueTextbox.AppendText(path);
            }
            queueTextbox.ScrollToEnd();
        }

        private void addToOutputbox(string output)
        {
            outputBox.AppendText(output + "\r\n");
            outputBox.ScrollToEnd();
        }

        private void updateTimer()
        {
            //elapsedTime.Content;
        }

    }
}


public class AsyncDia
{
    // A list with paths to .dat files
    public static List<Label> jobs = new List<Label> { };
    public static List<string> diana_version = new List<string> { };

    public static async Task<string> add_job_(Label path, string version)
    {
        var outp = await start_process(path, version);
        return outp;

    }

    public static async Task<string> start_process(Label path, string version)
    {

        // Path to .dat file
        var path_dat = path;

        // Directory root
        string root = Directory.GetParent(path_dat.Content.ToString()).ToString();

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
                version = "DEVELOPMENT 10.0skype";
                break;
        }
        
        TextReader tr = new StreamReader(stream);
        string solver_file = tr.ReadToEnd();
  
        // Append information to solver.bat
        string title = System.IO.Path.GetFileName(path_dat.Content.ToString());
        title = title.Remove(title.Length - 4);

        //solver_file += String.Format("\r\ncd {0}\r\n", root);
        solver_file += String.Format("\r\ncd {0}\r\ntitle Diana {1} Command Box - PROJECT: {2}", root, version, title);

        solver_file += "\r\ntimeout 5";
        solver_file += String.Format("\r\n    diana -m {0} {1}.ff", title, title);
        solver_file += "\r\ntimeout 5";

        var solv_f = new StreamWriter(System.IO.Path.Combine(root, "solver.bat"));
        solv_f.Write(solver_file);
        solv_f.Close();

        string filename = System.IO.Path.Combine(root, "solver.bat");
        await RunProcessAsync(filename);

        // Remove Filos File
        DirectoryInfo dir = new DirectoryInfo(root);
        FileInfo[] ff_files = dir.GetFiles("*.ff");
        
        foreach (FileInfo filos in ff_files)
        {
            try
            {
                filos.Attributes = FileAttributes.Normal;
                File.Delete(filos.FullName);
            }
            catch { }
        }
        try
        {
            File.Delete(System.IO.Path.Combine(root, "solver.bat"));
        }
        catch { }

        return String.Format("Finished task : {0}", System.IO.Path.Combine(root, title));

    }

    static Task RunProcessAsync(string fileName)
        // Async method for waiting for the commandbox to be finished.
    {
        // there is no non-generic TaskCompletionSource
        var tcs = new TaskCompletionSource<bool>();

        var process = new Process
        {
            StartInfo = { FileName = fileName },
            EnableRaisingEvents = true
        };

        process.Exited += (sender, args) =>
        {
            tcs.SetResult(true);
            process.Dispose();
        };

        process.Start();

        return tcs.Task;
    }
}