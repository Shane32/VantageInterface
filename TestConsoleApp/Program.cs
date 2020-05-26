using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VantageInterface;

namespace TestConsoleApp
{
    class Program
    {
        static VControl vControl;

        static void Main(string[] args)
        {
            //Task.Run(async () =>
            //{
            //    await Task.Delay(50);
            //    Console.WriteLine("Test1");
            //    Task.Run(() =>
            //    {
            //        Console.WriteLine("Start sub");
            //        Thread.Sleep(5000);
            //        Console.WriteLine("End sub");
            //    });
            //    Console.WriteLine("Test2; press enter to exit");
            //    Console.ReadLine();

            //}).Wait();
            //return;


            MainAsync(args).Wait();
        }

        static async Task MainAsync(string[] args)
        {
            //await Task.Delay(1);
            vControl = new VControl("172.18.0.9");

            vControl.LoadUpdate += (int vid, float percent) =>
            {
                Console.WriteLine($"Load #{vid} set to {percent}%");
            };
            vControl.TaskUpdate += (int vid, int state) =>
            {
                Console.WriteLine($"Task #{vid} new state {state}");
            };
            vControl.ButtonUpdate += (int vid, ButtonModes mode) =>
            {
                Console.WriteLine($"Button #{vid} {(mode == ButtonModes.Press ? "Pressed" : "Released")}");
            };
            vControl.LedUpdate += LedUpdateFn;

            //await vControl.ConnectAsync();
            vControl.Connect();
            Console.WriteLine("Getting load 219");
            //vControl.UpdateLoad(219);
            Console.WriteLine(await vControl.Get.LoadAsync(219));
            Console.ReadLine();
            Console.WriteLine("Getting led 1591");
            Console.WriteLine(await vControl.Get.LedAsync(1591));

            Console.WriteLine("Setting to 0%");
            vControl.Set.Load(219, 0);

            Console.ReadLine();

            Console.WriteLine("Setting to 50%");
            vControl.Set.Load(219, 50);

            Console.ReadLine();

            Console.WriteLine("Setting to 200%");
            vControl.Set.Load(219, 200);

            Console.ReadLine();

            vControl.Close();


        }

        static void LedUpdateFn(int vid, LedState state) {
            Console.WriteLine($"LED #{vid} set to state {state.State}");
        }
    }
}
