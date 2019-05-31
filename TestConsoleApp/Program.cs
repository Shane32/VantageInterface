using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VantageInterface;

namespace TestConsoleApp
{
    class Program
    {
        static VControl vControl;

        static void Main(string[] args)
        {
            vControl = new VControl("172.18.0.9");

            vControl.LoadUpdate += (int vid, float percent) =>
            {
                Console.WriteLine($"Load #{vid} set to {percent}%");
            };
            vControl.LedUpdate += LedUpdateFn;

            vControl.Connect();

            Console.ReadLine();

            vControl.SetLoad(219, 0);

            Console.ReadLine();

            vControl.SetLoad(219, 50);

            Console.ReadLine();

            vControl.SetLoad(219, 200);

            Console.ReadLine();

            vControl.Close();


        }

        static void LedUpdateFn(int vid, float number) {
            Console.WriteLine($"LED #{vid} set to {number}");
        }
    }
}
