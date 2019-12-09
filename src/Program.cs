using System;
using System.Threading.Tasks;

namespace PoeLogThing
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("PoE Log Watcher");
            Console.WriteLine("---------------\r\n");

            var watchTask =
                new LogWatcher().WatchAsync(
                    @"C:\Program Files (x86)\Grinding Gear Games\Path of Exile\logs\Client.txt");


            Console.ReadKey(true);
            
            // Can also just await it...
            // await watchTask;
        }
    }
}
