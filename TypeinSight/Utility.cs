using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeInSight
{
    class LoggingUtility
    {
        private static StreamWriter sw = null;

        public LoggingUtility()
        {
            if (sw == null)
                sw = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\LogFile.txt", true);
        }

        ~LoggingUtility()
        {
            if(sw!=null)
            {
                sw.Close();
            }
        }
        public void WriteToLog(string Message)
        {            
            try
            {
                sw.WriteLine(DateTime.Now.ToString() + ": " + Message);
                sw.Flush();
            }
            catch { }
        }
    }
}
