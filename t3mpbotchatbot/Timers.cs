using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace t3mpbotchatbot
{
    class Timers
    {

        private bool StoppingOn = false;
        private bool StartedOn = false;
        private bool StoppingOff = false;
        private bool StartedOff = false;
        private bool StoppingXP = false;
        private bool StartedXP = false;
        private Thread TimerOn;
        private Thread TimerOff;
        private Thread TimerXP;
        private readonly int IntervalMinutesOn = 60;
        private readonly int IntervalMinutesOff = 240;
        private readonly int IntervalMinutesXP = 1;
        object _lock = Main._lock;
        List<string> OnlineUsers = Main.OnlineUsers;
        List<string> OfflineUsers = Main.OfflineUsers;

        public Timers(int IntervalMinutesOn, int IntervalMinutesOff, int IntervalMinutesXP)
        {
            this.IntervalMinutesOn = IntervalMinutesOn;
            this.IntervalMinutesOff = IntervalMinutesOff;
            this.IntervalMinutesXP = IntervalMinutesXP;
        }

        public void StartOn()
        {
            if (StartedOn)
                return;

            TimerOn = new Thread(new ThreadStart(TimerThreadOn));
            TimerOn.Start();

            StartedOn = true;
        }

        public void StartOff()
        {
            if (StartedOff)
                return;

            TimerOff = new Thread(new ThreadStart(TimerThreadOff));
            TimerOff.Start();

            StartedOff = true;
        }

        public void StartXP()
        {
            if (StartedXP)
                return;

            Console.WriteLine("Started XP Timer.");
            TimerXP = new Thread(new ThreadStart(TimerThreadXP));
            TimerXP.Start();

            StartedXP = true;
        }

        public void StopOn()
        {
            if (!StartedOn)
                return;

            StoppingOn = true;
        }

        public void StopOff()
        {
            if (!StartedOff)
                return;

            StoppingOff = true;
        }

        public void StopXP()
        {
            if (!StartedXP)
                return;

            StoppingXP = true;
        }

        private void TimerThreadOn()
        {
            DateTime nextTick = DateTime.Now.AddMinutes(IntervalMinutesOn);

            while (true)
            {
                if (StoppingOn)
                    return;
                if (DateTime.Now < nextTick)
                {
                    Thread.Sleep(1000);
                    continue;
                }

                lock(_lock)
                {
                    foreach (string Online in OnlineUsers)
                    {
                        int position = Online.IndexOf(".");
                        string channel = Online.Substring(0, position);
                        string user = Online.Substring(position + 1);
                        //Do the thing
                        Main.AddEnergy(channel, user);
                    }
                    Console.WriteLine("Added Energy!");
                }

                nextTick = DateTime.Now.AddMinutes(IntervalMinutesOn);
            }
        }

        private void TimerThreadOff()
        {
            DateTime nextTick = DateTime.Now.AddMinutes(IntervalMinutesOff);

            while (true)
            {
                if (StoppingOff)
                    return;
                if (DateTime.Now < nextTick)
                {
                    Thread.Sleep(1000);
                    continue;
                }

                lock (_lock)
                {
                    foreach (string Offline in OfflineUsers)
                    {
                        int position = Offline.IndexOf(".");
                        string channel = Offline.Substring(0, position);
                        string user = Offline.Substring(position + 1);
                        //Do the thing
                        Main.AddEnergy(channel, user);
                    }
                }

                nextTick = DateTime.Now.AddMinutes(IntervalMinutesOff);
            }
        }

        private void TimerThreadXP()
        {
            DateTime nextTick = DateTime.Now.AddMinutes(IntervalMinutesXP);

            while (true)
            {
                if (StoppingXP)
                    return;
                if (DateTime.Now < nextTick)
                {
                    Thread.Sleep(1000);
                    continue;
                }

                lock (_lock)
                {
                    foreach (string Online in OnlineUsers)
                    {
                        int position = Online.IndexOf(".");
                        string channel = Online.Substring(0, position);
                        string user = Online.Substring(position + 1);
                        //Do the thing
                        Main.AddXP(channel, user);
                        Thread.Sleep(20);
                        Main.LvlUp(channel, user);
                        Thread.Sleep(20);
                    }
                }
                
                nextTick = DateTime.Now.AddMinutes(IntervalMinutesXP);
            }
        }
    }
}
