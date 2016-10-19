using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ServerWorker
{
    class Timer
    {
        private Stopwatch sw = new Stopwatch();

        public void start()
        {
            sw.Start();
        }

        public void stop()
        {
            sw.Stop();
            sw.Reset();
        }

        public string time()
        {
                     
            TimeSpan ts = sw.Elapsed;
         
            string elapsedTime = String.Format("{0:00}:{1:00}", ts.Hours, ts.Minutes);
            return elapsedTime;
        }
    }
}
