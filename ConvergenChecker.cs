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
            System.Threading.Thread.Sleep(120000);
            while (AsyncDia.stop_convergence)
            {
                //Debug.WriteLine("still trying");
                try
                {
                    using (FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (StreamReader rd = new StreamReader(stream))
                        {
                            var content = rd.ReadToEnd();
                            var index = content.LastIndexOf("TERMINATED, NO CONVERGENCE AFTER");
                            var substring = content.Substring(index - 300, 300);
                            Debug.Write(substring);
                            var index_energy = substring.LastIndexOf("RELATIVE ENERGY VARIATION");
                            var index_force = substring.LastIndexOf("RELATIVE OUT OF BALANCE FORCE");
                            var index_displ = substring.LastIndexOf("RELATIVE DISPLACEMENT VARIATION");
                            Double val_energy = 1e3;
                            Double val_force = 1e3;
                            Double val_displ = 1e3;

                            // >= 0 substring exists
                            if (index_energy >= 0) 
                            {
                                val_energy = Double.Parse(substring.Substring(index_energy + 35, 9).Replace('.', ','), System.Globalization.NumberStyles.Float);
                            }
                            if (index_force >= 0)
                            {
                                val_force = Double.Parse(substring.Substring(index_force + 35, 9).Replace('.', ','), System.Globalization.NumberStyles.Float);
                            }
                            if (index_displ >= 0)
                            {
                                val_displ = Double.Parse(substring.Substring(index_displ + 35, 9).Replace('.', ','), System.Globalization.NumberStyles.Float);
                            }
                            var val = Math.Min(Math.Min(val_displ, val_force), val_energy);

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
                System.Threading.Thread.Sleep(150000); // Sleep 150 seconds
            }
        }
    }
}