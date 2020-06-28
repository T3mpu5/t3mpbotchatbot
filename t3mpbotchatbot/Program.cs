using System;

namespace t3mpbotchatbot
{
    class Program
    {
        static void Main(string[] args)
        {
            Main bot = new Main();
            bot.Connect();
            Timers timers = new Timers(60, 240, 1);
            timers.StartOn();
            timers.StartOff();
            timers.StartXP();

            Console.ReadLine();

            bot.Disconnect();
        }
    }
}
