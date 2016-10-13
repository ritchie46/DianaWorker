﻿using System;
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

namespace ServerWorker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {   
        public MainWindow()
        {
            InitializeComponent();
        }

        public Label Path_dat
        {
            get
            {
                return this.path_dat;
            }

            set
            {
                this.path_dat = value;
            }
        }

        private void add_path(object sender, RoutedEventArgs e)
        {

            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.DefaultExt = ".dat";
            dialog.Filter = "*.dat | *.DAT";
            var succes = dialog.ShowDialog();
                        
            if (succes == true)
            {

                // Set the value to the path_dat label
                path_dat.Content = dialog.FileName;
            }
        }

        private async void add_job(object sender, RoutedEventArgs e)
        {

            Debug.WriteLine("button clicked");

            string outp = await AsyncDia.add_job_(path_dat);
            output_box.AppendText(outp);
        }
    }

}


public class AsyncDia
{
    
    public static async Task<string> add_job_(Label path)
    {
        Debug.WriteLine("start async");
        var outp = await start_process(path);
        return outp;

    }

    public static async Task<string> start_process(Label path)
    {

        //System.Threading.Thread.Sleep(10000);
        var path_dat = path;

        string root = System.IO.Directory.GetParent(path_dat.Content.ToString()).ToString();
        String solver_file = System.IO.File.ReadAllText("C:/Users/vik/Dropbox/Code/Wintrack/pydia/foundation/res/solver.bat");
        string title = System.IO.Path.GetFileName(path_dat.Content.ToString());
        title = title.Remove(title.Length - 4);

        solver_file += String.Format("\n    diana -m {0}", System.IO.Path.Combine(root, title));

        var solv_f = new System.IO.StreamWriter(System.IO.Path.Combine(root, "solver.bat"));
        solv_f.Write(solver_file);
        solv_f.Close();

        string filename = System.IO.Path.Combine(root, "solver.bat");
        await RunProcessAsync(filename);
        //Process.Start(System.IO.Path.Combine(root, "solver.bat"));

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