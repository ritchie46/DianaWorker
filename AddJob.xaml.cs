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
        private bool _jobs_running;
        public bool jobs_running
        {
            set
            {
                _jobs_running = value;
            }
            get
            {
                return _jobs_running;
                
            }
        }
       
        public bool path_chosen = false; 
        public List<string> wait_list = new List<string> { };
        
        public MainWindow()
        {
            InitializeComponent();
            jobs_running = false;
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
     
                // Add job to wait list. This makes sure the correct jobs are plotted
                wait_list.Add("\n" + path_dat.Content.ToString());
                refresh_job_list();

                AsyncDia.jobs.Add(path_dat);


                if (!jobs_running)
                {   
                    // reset job list
                    AsyncDia.jobs = new List<Label> { };
                    AsyncDia.jobs.Add(path_dat);

                    output_box.AppendText("\nStarting first job.");
                    jobs_running = true;
                    await first_call();
                    output_box.AppendText("\nAll jobs finished");
                    jobs_running = false;
                }
            }
        }

        private void remove_last(object sender, RoutedEventArgs e)
            // Remove last task from the queue
        {
            if (wait_list.Count > 0)
            {
                wait_list.RemoveAt(wait_list.Count - 1);
                AsyncDia.jobs.RemoveAt(AsyncDia.jobs.Count - 1);
                refresh_job_list();      
            }
        }

        private async Task first_call()
            // Starts working on the DIANA task queue.
        {
            int count = 0;
            while (count < AsyncDia.jobs.Count)
            {
                output_box.AppendText("\nStarting " + path_dat.Content.ToString());
                string outp = await AsyncDia.add_job_(AsyncDia.jobs[count]);
                output_box.AppendText("\n" + outp);

                // remove path from tasklist
                wait_list.RemoveAt(0);

                refresh_job_list();
                count++;
            }
        }

        public void refresh_job_list()
            // Refreshes the job list/ task queue textbox
        {
            // Refresh job list
            job_list.Document.Blocks.Clear();
            foreach (string path in wait_list)
            {
                job_list.AppendText(path);
            }
            job_list.ScrollToEnd();
        }

    }

}


public class AsyncDia
{
    // A list with paths to .dat files
    public static List<Label> jobs = new List<Label> { };


    public static async Task<string> add_job_(Label path)
    {
        var outp = await start_process(path);
        return outp;

    }

    public static async Task<string> start_process(Label path)
    {

        // Path to .dat file
        var path_dat = path;

        // Directory root
        string root = Directory.GetParent(path_dat.Content.ToString()).ToString();

        // Read solver.bat from resources
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ServerWorker.Resources.solver.bat");
 
        TextReader tr = new StreamReader(stream);
        string solver_file = tr.ReadToEnd();
  
        // Append information to solver.bat
        string title = System.IO.Path.GetFileName(path_dat.Content.ToString());
        title = title.Remove(title.Length - 4);

        solver_file += String.Format("\ncd {0}", root);
        solver_file += String.Format("\n    diana -m {0}", System.IO.Path.Combine(root, title));
        solver_file += "\ntimeout 5";

        var solv_f = new System.IO.StreamWriter(System.IO.Path.Combine(root, "solver.bat"));
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