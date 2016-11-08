using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace ServerWorker
{
    class ConvergenceChecker
    {
        public static bool stopMe = false;

        public static void check()
        {
            var path = Path.Combine(AsyncDia.root, AsyncDia.title);
            path += ".out";
            while (AsyncDia.stop_convergence)
            {
                Debug.WriteLine("still trying");
                try
                {
                    using (FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (StreamReader rd = new StreamReader(stream))
                        {
                            var content = rd.ReadToEnd();
                            var index = content.LastIndexOf("TERMINATED, NO CONVERGENCE AFTER");
                            // - 50 should be the index of the relative energy variation
                            var val_str = content.Substring(index - 50, 9);
                            val_str = val_str.Replace(".", ",");
                            var val = Double.Parse(val_str, System.Globalization.NumberStyles.Float);

                            if (val > AsyncDia.convergence_value)
                            {
                                MainWindow.cancelNowRunning = true;
                                try
                                {
                                    using (StreamWriter rw = File.AppendText(path))
                                    {
                                        rw.WriteLine("Calculation stopped by the Worker as the stop criteria was met");
                                    }
                                }
                                catch (Exception)
                                {
                                    Debug.WriteLine("Could not write to .out file");
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    Debug.WriteLine("Could not find convergence value");
                }

                if (stopMe)
                {
                    stopMe = false;
                    break;
                }
                System.Threading.Thread.Sleep(15000);
            }
        }
    }
}