using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Dummy
{
    class run
    {
        static void Main(string[] args)
        {
#if DEBUG
            string idpw = Console.ReadLine();
            Dummy dummy = new Dummy();
            dummy.Start(idpw);
#else
            string idpw = args[0];
            Dummy dummy = new Dummy();
            dummy.Start(idpw);
#endif
        }
    }
}