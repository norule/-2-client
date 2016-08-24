using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Timers;
using System.Threading;
//using static Lognamespace.cLog;
namespace Lognamespace
{
    public static class cLog
    {
        static Queue<string> message = new Queue<string>();
        static StreamWriter w;
        static object otest = new object();
        public static void Log(string logMessage)
        {
            lock (otest)
            {
                w = File.AppendText(DateTime.Now.ToLongDateString() + "log.txt");
                w.Write("{1} {0}", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
                w.Write("  : ");
                w.WriteLine(logMessage);
                w.Close();
            }
        }
    }
}