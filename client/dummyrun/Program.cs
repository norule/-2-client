using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace dummyrun
{
    class Program
    {
        static List<Process> dummys = new List<Process>();
        static void Main(string[] args)
        {
            int inx = 1;
            int max = 1;

            Console.WriteLine("how many dummy? :");
            while (true)
            {
                string inputmax = Console.ReadLine();
                if (Int32.TryParse(inputmax, out max))
                {
                    for (inx = 1; inx <= max; inx++)
                    {
                        Process dm = new Process();

                        dm.StartInfo.FileName = "C:\\Users\\askdnwn\\Desktop\\client\\dummyrun\\Dummy\\Dummy.exe";
                        dm.StartInfo.Arguments = "Dummy" + inx;
                        dm.Start();
                        dummys.Add(dm);
                        Console.Write("...");
                        Thread.Sleep(500);
                    }
                    break;
                }
                else
                {
                    Console.WriteLine("try agin");
                }
            }

            Console.WriteLine("\ndummy running(exit = e)");
            while (true)
            {
                string e = Console.ReadLine();
                if (e == "e")
                {
                    for(int i = 0; i < dummys.Count; i++)
                    {
                        dummys[i].Kill();
                    }
                    break;
                }
            }
        }
    }
}
