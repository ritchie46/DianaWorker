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
            while (AsyncDia.stop_convergence)
            {
                var path = Path.Combine(AsyncDia.root, AsyncDia.title);
                // for testing
                //path = "C:\\Users\\vik\\Desktop\\leeg\\ong_char.out";
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

                            }
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                    System.Threading.Thread.Sleep(10000);
                }
                Debug.WriteLine("hier");
                System.Threading.Thread.Sleep(5000);

                if (stopMe)
                {
                    stopMe = false;
                    break;
                }
            }

        }
    }
}
