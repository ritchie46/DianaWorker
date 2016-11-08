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

        public void output(string content)
        {
            addToOutputbox(content);
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
                var dialog = new Dialog();
                dialog.ShowDialog();
                if (dialog.DialogResult == true)
                {
                    // convert nullabe bool to bool
                    var stop_conv = dialog.radioButtonConv.IsChecked ?? false;
                    var mailAdress = dialog.radioButtonMail.IsChecked ?? false;
                    var insert = dialog.radioButtonInsert.IsChecked ?? false;

                    if (insert)
                    {
                        var queueIndex = 0;
                        Int32.TryParse(dialog.TextBoxInsert.Text, out queueIndex);
                        insertJob(sender, e, queueIndex, dialog);
                    }
                    else
                    {
                        var version = diana_version.SelectedItem.ToString().Substring(diana_version.SelectedItem.ToString().Length - 4);

                        // Add job to queue. This makes sure the correct jobs are printed.
                        queue.Add(String.Format("{0} [{1}]", path_dat.Content.ToString(), version));
                        refreshQueueTextbox();

                        if (!jobsRunning)
                        {
                            // reset job list
                            AsyncDia.jobs = new List<string>();
                            AsyncDia.jobs.Add(path_dat.Content.ToString());
                            AsyncDia.diana_version = new List<string>();
                            AsyncDia.diana_version.Add(version);
                            AsyncDia.stop_conv = new List<bool>();
                            AsyncDia.stop_conv.Add(stop_conv);
                            AsyncDia.conv_val = new List<double>();
                            AsyncDia.conv_val.Add(dialog.conv_val);
                            AsyncDia.email = new List<string>();
                            if (mailAdress)
                            {
                                AsyncDia.email.Add(dialog.TextBoxMail.Text);
                            }
                            else
                            {
                                AsyncDia.email.Add("none");
                            }

                            addToOutputbox("Starting first job.\r\n");
                            jobsRunning = true;
                            await firstCall();
                            addToOutputbox("All jobs finished");
                            jobsRunning = false;
                        }
                        else
                        {
                            AsyncDia.jobs.Add(path_dat.Content.ToString());
                            AsyncDia.diana_version.Add(version);
                            AsyncDia.stop_conv.Add(stop_conv);
                            AsyncDia.conv_val.Add(dialog.conv_val);
                            if (mailAdress)
                            {
                                AsyncDia.email.Add(dialog.TextBoxMail.Text);
                            }
                            else
                            {
                                AsyncDia.email.Add("none");
                            }
                        }
                    }
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
                AsyncDia.stop_conv.RemoveAt(AsyncDia.diana_version.Count - 1);
                AsyncDia.conv_val.RemoveAt(AsyncDia.diana_version.Count - 1);
                AsyncDia.email.RemoveAt(AsyncDia.diana_version.Count - 1);
                refreshQueueTextbox();      
            }
        }

        private void insertJob(object sender, RoutedEventArgs e, int insert, Dialog dialog)
        {
            if (!jobsRunning)
            {
                addJob(sender, e);
            }
            else
            {
            // convert nullabe bool to bool
            var stop_conv = dialog.radioButtonConv.IsChecked ?? false;
            var mailAdress = dialog.radioButtonMail.IsChecked ?? false;

            if (insert > 0)  // the input is correct
            {
                if (insert + AsyncDia.count >= AsyncDia.jobs.Count)
                {
                        MessageBox.Show("The index is larger than the queue.");
                        addJob(sender, e);
                }
                else
                {
                    var version = diana_version.SelectedItem.ToString().Substring(diana_version.SelectedItem.ToString().Length - 4);

                    // Add job to queue. This makes sure the correct jobs are printed.
                    queue.Insert(insert, String.Format("{0}[{1}]", path_dat.Content.ToString(), version));
                    refreshQueueTextbox();

                    AsyncDia.jobs.Insert(insert + AsyncDia.count, path_dat.Content.ToString());
                    AsyncDia.diana_version.Insert(insert + AsyncDia.count, version);
                    AsyncDia.stop_conv.Insert(insert + AsyncDia.count, stop_conv);
                    AsyncDia.conv_val.Insert(insert + AsyncDia.count, dialog.conv_val);
                    if (mailAdress)
                    {
                        AsyncDia.email.Add(dialog.TextBoxMail.Text);
                    }
                    else
                    {
                        AsyncDia.email.Add("none");
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
                var input = new inputDialog
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
                            AsyncDia.stop_conv.RemoveAt(input.queueIndex);
                            AsyncDia.conv_val.RemoveAt(input.queueIndex);
                            AsyncDia.email.RemoveAt(input.queueIndex);
                        }
                    }
                }
            }
        }

        private void quitCurrentJob(object sender, RoutedEventArgs e)
        {
            if (jobsRunning)
            {
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
                addToOutputbox(String.Format("Starting {0} at {1}", 
                    queue[0],
                    DateTime.Now.ToShortTimeString()));

                string outp = null;
                                
                // Make sure the job will get a license and will not be skipped.   
                while (true)
                {
                    outp = await AsyncDia.add_job_(AsyncDia.jobs[count], AsyncDia.diana_version[count], AsyncDia.stop_conv[count], AsyncDia.conv_val[count]);


                    // wait 4 seconds, to make sure .out file is created.
                    System.Threading.Thread.Sleep(4000);

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
                    catch (IndexOutOfRangeException)
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


