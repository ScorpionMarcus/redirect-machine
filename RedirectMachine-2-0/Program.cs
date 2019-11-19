using Gizmo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedirectMachine_2_0
{
    class Program
    {
        static void Main(string[] args)
        {
            Gremlin.Init(Environment.MachineName);
            Gremlin.EmailTo = "marcus.legault@scorpion.co";

            Console.WriteLine("Starting redirect machine...");

            string root = @"S:\M-R\Marcus LeGault\Redirect Machine";
            RedirectJobFinder jobs = new RedirectJobFinder(root);

            int jobCount = jobs.returnJobCount();

            jobs.Run();

            Console.WriteLine("Done!");

            Gremlin.Close();
        }
    }
}
