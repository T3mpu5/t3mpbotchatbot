using System;
using System.IO;
using System.Xml;
using System.Threading;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;
using TwitchLib.Client.Models;
using TwitchLib.Client.Interfaces;
using TwitchLib.Communication.Models;
using TwitchLib.Communication.Clients;
using System.Xml.Linq;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Text.RegularExpressions;
using TwitchLib.Api.Core.Models.Undocumented.CSStreams;
using System.Data.SqlTypes;

namespace t3mpbotchatbot
{
    public class ScoresClass
    {
        Adventure adventure = new Adventure();
        public string Channel = null;
        public string User = null;
        public int XP = 0;
        public int STR = 0;
        public int END = 0;
        public int INT = 0;
        public int DEX = 0;
        public ScoresClass(string channel, string user)
        {
            Channel = channel;
            User = user;
            XP = Main.LookUpXP(channel, user);
            STR = adventure.GetPlayerStr(channel, user);
            END = adventure.GetPlayerEnd(channel, user);
            INT = adventure.GetPlayerInt(channel, user);
            DEX = adventure.GetPlayerDex(channel, user);
        }
    }

    public class Main
    {
        ConnectionCredentials credentials = new ConnectionCredentials(TwitchInfo.BotUsername, TwitchInfo.BotToken);
        public static TwitchClient client;
        public static List<string> OnlineUsers = new List<string>();
        public static List<string> OfflineUsers = new List<string>();
        public static Object _lock = new Object();
        public static Object _lockxp = new Object();
        public static Object _lockstats = new Object();
        public static Object _lockoutl = new Object();
        public static Object _lockloot = new Object();
        public static Object _lockchann = new Object();
        public static Object _lockadv = new Object();
        public static Object _lockrift = new Object();
        public static Object _lockinfo = new Object();
        Random random = new Random(Guid.NewGuid().GetHashCode());
        private static readonly Regex regex = new Regex(@"^\d+$");
        Adventure adventure = new Adventure();
        Battle battle = new Battle();
        Shop shop = new Shop();
        Rift rift = new Rift();
        public bool PickUp = false;
        public string PickUpLoot = null;

        internal void Connect()
        {
            Console.WriteLine("Connecting...");

            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 1200,
                ThrottlingPeriod = TimeSpan.FromSeconds(60)
            };
            WebSocketClient customClient = new WebSocketClient(clientOptions);
            client = new TwitchClient(customClient);
            client.Initialize(credentials, TwitchInfo.ChannelName);
            client.OnLog += Client_OnLog;
            client.OnConnectionError += Client_OnConnectionError;
            client.OnMessageReceived += Client_OnMessageReceived;
            client.OnWhisperReceived += Client_OnWhisperReceived;
            client.OnUserJoined += Client_OnUserJoined;
            client.OnUserLeft += Client_OnUserLeft;
            client.OnJoinedChannel += Client_OnJoinedChannel;
            client.OnLeftChannel += Client_OnLeftChannel;
            client.OnDisconnected += Client_OnDisconnected;
            client.Connect();
            shop.Refresh();
            System.Threading.Thread.Sleep(7500);
            XDocument CH = XDocument.Load(@"Data\Channels.xml");
            XElement ele = CH.Element("channels").Element("joined");
            try
            {
                foreach (XAttribute joined in ele.Attributes())
                {
                    client.JoinChannel(joined.Name.ToString());
                    Thread.Sleep(500);
                }
            }
            catch
            {

            }
            rift.Connect();
        }

        private void Client_OnWhisperReceived(object sender, OnWhisperReceivedArgs e)
        {
            if (e.WhisperMessage.Message.StartsWith("!"))
            {
                string[] args = e.WhisperMessage.Message.Split(' ');
                XDocument CH = XDocument.Load(@"Data\Channels.xml");
                XElement ele = CH.Element("channels").Element("joined");
                if (args[0].ToLower() == "!oauth")
                {
                    if (ele.Attribute(e.WhisperMessage.Username) != null)
                    {
                        ele.SetAttributeValue(e.WhisperMessage.Username.ToLower(), args[1]);
                        CH.Save(@"Data\Channels.xml");
                        client.SendWhisper(e.WhisperMessage.Username, "OAuth Token has been saved, thank you.");
                        rift.Listen();
                    }
                    else
                    {
                        client.SendWhisper(e.WhisperMessage.Username, "T3mpbot hasn't been installed on your channel!");
                    }
                }
            }
        }

        private void Client_OnLeftChannel(object sender, OnLeftChannelArgs e)
        {
            Console.WriteLine("Left Channel: " + e.Channel);
        }

        private void Client_OnDisconnected(object sender, TwitchLib.Communication.Events.OnDisconnectedEventArgs e)
        {
            Console.WriteLine("Disconnected");
            Connect();
        }

        private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            adventure.ClearPlayers(e.Channel.ToUpper());
            client.SendMessage(e.Channel, "/color GoldenRod");
        }

        private void Client_OnUserLeft(object sender, OnUserLeftArgs e)
        {
            string channel = e.Channel.ToUpper();
            string user = e.Username.ToUpper();
            lock (_lock)
            {
                OnlineUsers.Remove(channel + "." + user);
                OfflineUsers.Add(channel + "." + user);
            }

        }

        private void Client_OnUserJoined(object sender, OnUserJoinedArgs e)
        {
            string channel = e.Channel.ToUpper();
            string user = e.Username.ToUpper();
            if (LookUpXP(channel, user) == 0)
            {
                WriteNewXP(channel, user);
            }
            lock (_lock)
            {
                OfflineUsers.Remove(channel + "." + user);
                OnlineUsers.Add(channel + "." + user);
            }
        }

        public static void CheckNoXP(string channel, string user)
        {
            if (LookUpXP(channel, user) == 0)
            {
                WriteNewXP(channel, user);
            }
            if (CheckHP(channel, user) == 0)
            {
                WriteNewStats(channel, user);
            }
        }

        public static void WriteNewXP(string channel, string user)
        {
            lock (_lockxp)
            {
                XDocument NStats = XDocument.Load(@"Data\XP.xml");
                XElement users = NStats.Element("users");
                int XPnew = 1;
                users.Add(new XElement(GetTopic(channel, user),
                    new XAttribute("XP", XPnew.ToString()),
                    new XAttribute("Lvl", "1"),
                    new XAttribute("OldLvl", "1"),
                    new XAttribute("SkPoints", "0"),
                    new XAttribute("Energy", "3"),
                    new XAttribute("MaxEnergy", "3"),
                    new XAttribute("OptIn", "No")));
                NStats.Save(@"Data\XP.xml");
                WriteNewStats(channel, user);
            }
        }

        private static void WriteNewStats(string channel, string user)
        {
            lock (_lockstats)
            {
                XDocument NStats = XDocument.Load(@"Data\Stats.xml");
                XElement users = NStats.Element("stats");
                users.Add(new XElement(GetTopic(channel, user),
                    new XAttribute("HP", "100"),
                    new XAttribute("Strength", "5"),
                    new XAttribute("Archery", "5"),
                    new XAttribute("Speed", "5"),
                    new XAttribute("Endurance", "5"),
                    new XAttribute("Intelligence", "5"),
                    new XAttribute("Dexterity", "5"),
                    new XAttribute("Faith", "5"),
                    new XAttribute("Looting", "5")));
                NStats.Save(@"Data\Stats.xml");
            }
        }

        private static void AddNewStats()
        {
            lock (_lockstats)
            {
                XDocument NStats = XDocument.Load(@"Data\Stats.xml");
                XElement users = NStats.Element("stats");
                foreach (XElement ele in users.Elements())
                {
                    ele.Add(new XAttribute("Faith", "5"),
                        new XAttribute("Speed", "5"));
                }
                NStats.Save(@"Data\Stats.xml");
            }
        }

        public static int LookUpXP(string channel, string user)
        {
            lock (_lockxp)
            {
                XElement XP2 = XElement.Load(@"Data\XP.xml");
                string gotXP = null;
                foreach (XElement ele in XP2.Descendants(GetTopic(channel, user)))
                {
                    gotXP = ele.Attribute("XP").Value.ToString();
                }
                int? thisXP = Convert.ToInt32(gotXP);
                return Convert.ToInt32(thisXP);
            }
        }

        public static int CheckHP(string channel, string user)
        {
            XElement stats = XElement.Load(@"Data\Stats.xml");
            string gotHP = null;
            foreach (XElement ele in stats.Descendants(GetTopic(channel, user)))
            {
                gotHP = ele.Attribute("HP").Value.ToString();
            }
            int? thisXP = Convert.ToInt32(gotHP);
            return Convert.ToInt32(thisXP);
        }

        public static int LookUpLvl(string channel, string user)
        {
            double Calc = 0.18 * (Math.Sqrt(LookUpXP(channel, user))) + 1;
            int Lvl = Convert.ToInt32(Math.Floor(Calc));
            return Lvl;
        }

        public int LookUpNextLvl(string channel, string user)
        {
            int Lvl = LookUpLvl(channel, user);
            int XPLeft = Convert.ToInt32(Math.Floor(Math.Pow((Lvl / 0.18), 2) - LookUpXP(channel, user)));
            return XPLeft;
        }

        public static string GetTopic(string channel, string user)
        {
            XElement Chan = XElement.Load(@"Data\Channels.xml");
            string Global = null;
            foreach (XElement ele in Chan.Descendants("Channel." + channel))
            {
                Global = ele.Attribute("Global").Value.ToString();
            }
            string topic;
            if (Global == null)
            {
                topic = channel + "." + user;
            }
            else
            {
                topic = "GLOBAL." + user;
            }
            return topic;
        }

        public static void LvlUp(string channel, string user)
        {
            lock (_lockxp)
            {
                XElement XP = XElement.Load(@"Data\XP.xml");
                string OldLvl = null;
                string NewLvl = null;
                string SkPoints = null;
                string OptIn = null;
                foreach (XElement ele in XP.Descendants(GetTopic(channel, user)))
                {
                    OldLvl = ele.Attribute("OldLvl").Value.ToString();
                    NewLvl = ele.Attribute("Lvl").Value.ToString();
                    try
                    {
                        SkPoints = ele.Attribute("SkPoints").Value.ToString();
                    }
                    catch
                    {
                        XDocument LvlUp = XDocument.Load(@"Data\XP.xml");
                        XElement users = LvlUp.Element("users").Element(GetTopic(channel, user));
                        users.SetAttributeValue("SkPoints", 0);
                        LvlUp.Save(@"Data\XP.xml");
                    }
                    try
                    {
                        OptIn = ele.Attribute("OptIn").Value.ToString();
                    }
                    catch
                    {
                        XDocument LvlUp = XDocument.Load(@"Data\XP.xml");
                        XElement users = LvlUp.Element("users").Element(GetTopic(channel, user));
                        users.SetAttributeValue("OptIn", "No");
                        LvlUp.Save(@"Data\XP.xml");
                    }
                }
                if (Convert.ToInt32(NewLvl) > Convert.ToInt32(OldLvl))
                {
                    XDocument LvlUp = XDocument.Load(@"Data\XP.xml");
                    XElement users = LvlUp.Element("users").Element(GetTopic(channel, user));
                    string Max = users.Attribute("MaxEnergy").Value;
                    users.SetAttributeValue("OldLvl", NewLvl);
                    if (Convert.ToInt32(NewLvl) > 100)
                    {
                        users.SetAttributeValue("SkPoints", Convert.ToString(Convert.ToInt32(SkPoints) + ((Convert.ToInt32(NewLvl) - Convert.ToInt32(OldLvl)) * 10)));
                    }
                    else
                    {
                        users.SetAttributeValue("SkPoints", Convert.ToString(Convert.ToInt32(SkPoints) + ((Convert.ToInt32(NewLvl) - Convert.ToInt32(OldLvl)) * 5)));
                    }
                    if ((Convert.ToInt32(NewLvl) % 50) == 0)
                    {
                        users.SetAttributeValue("MaxEnergy", Convert.ToInt32(Max) + 1);
                    }
                    users.SetAttributeValue("Energy", Max);
                    LvlUp.Save(@"Data\XP.xml");
                    if (OptIn == "Yes")
                    {
                        client.SendMessage(channel, "/me : " + user.ToLower() + " has leveled up to lvl (" + NewLvl + ")");
                    }
                }
            }
        }

        private List<ScoresClass> LeaderboardXP(string channel)
        {
            var ListofScores = new List<ScoresClass>();
            XDocument XP = XDocument.Load(@"Data\XP.xml");
            XElement ele = XP.Element("users");
            foreach (XElement usr in ele.Descendants())
            {
                if (usr.Name.ToString().Substring(0, usr.Name.ToString().IndexOf('.')).Contains(channel))
                {
                    CheckNoXP(channel, usr.Name.ToString().Substring(usr.Name.ToString().IndexOf('.') + 1));
                    ListofScores.Add(new ScoresClass(channel, usr.Name.ToString().Substring(usr.Name.ToString().IndexOf('.') + 1)));
                }
            }
            return ListofScores.OrderByDescending(r => r.XP).ToList();
        }

        private List<ScoresClass> LeaderboardStr(string channel)
        {
            var ListofScores = new List<ScoresClass>();
            XDocument XP = XDocument.Load(@"Data\XP.xml");
            XElement ele = XP.Element("users");
            foreach (XElement usr in ele.Descendants())
            {
                if (usr.Name.ToString().Substring(0, usr.Name.ToString().IndexOf('.')).Contains(channel))
                {
                    CheckNoXP(channel, usr.Name.ToString().Substring(usr.Name.ToString().IndexOf('.') + 1));
                    ListofScores.Add(new ScoresClass(channel, usr.Name.ToString().Substring(usr.Name.ToString().IndexOf('.') + 1)));
                }
            }
            return ListofScores.OrderByDescending(r => r.STR).ToList();
        }

        private List<ScoresClass> LeaderboardEnd(string channel)
        {
            var ListofScores = new List<ScoresClass>();
            XDocument XP = XDocument.Load(@"Data\XP.xml");
            XElement ele = XP.Element("users");
            foreach (XElement usr in ele.Descendants())
            {
                if (usr.Name.ToString().Substring(0, usr.Name.ToString().IndexOf('.')).Contains(channel))
                {
                    CheckNoXP(channel, usr.Name.ToString().Substring(usr.Name.ToString().IndexOf('.') + 1));
                    ListofScores.Add(new ScoresClass(channel, usr.Name.ToString().Substring(usr.Name.ToString().IndexOf('.') + 1)));
                }
            }
            return ListofScores.OrderByDescending(r => r.END).ToList();
        }

        private List<ScoresClass> LeaderboardInt(string channel)
        {
            var ListofScores = new List<ScoresClass>();
            XDocument XP = XDocument.Load(@"Data\XP.xml");
            XElement ele = XP.Element("users");
            foreach (XElement usr in ele.Descendants())
            {
                if (usr.Name.ToString().Substring(0, usr.Name.ToString().IndexOf('.')).Contains(channel))
                {
                    CheckNoXP(channel, usr.Name.ToString().Substring(usr.Name.ToString().IndexOf('.') + 1));
                    ListofScores.Add(new ScoresClass(channel, usr.Name.ToString().Substring(usr.Name.ToString().IndexOf('.') + 1)));
                }
            }
            return ListofScores.OrderByDescending(r => r.INT).ToList();
        }

        private List<ScoresClass> LeaderboardDex(string channel)
        {
            var ListofScores = new List<ScoresClass>();
            XDocument XP = XDocument.Load(@"Data\XP.xml");
            XElement ele = XP.Element("users");
            foreach (XElement usr in ele.Descendants())
            {
                if (usr.Name.ToString().Substring(0, usr.Name.ToString().IndexOf('.')).Contains(channel))
                {
                    CheckNoXP(channel, usr.Name.ToString().Substring(usr.Name.ToString().IndexOf('.') + 1));
                    ListofScores.Add(new ScoresClass(channel, usr.Name.ToString().Substring(usr.Name.ToString().IndexOf('.') + 1)));
                }
            }
            return ListofScores.OrderByDescending(r => r.DEX).ToList();
        }

        public static void AddXP(string channel, string user)
        {
            CheckNoXP(channel, user);
            lock (_lockxp)
            {
                XDocument XP = XDocument.Load(@"Data\XP.xml");
                string NewXP = null;
                string NewLvl = null;
                string Lvl = null;
                foreach (XElement ele in XP.Descendants(GetTopic(channel, user)))
                {
                    NewXP = Convert.ToString(Convert.ToInt32(ele.Attribute("XP").Value) + 2);
                    NewLvl = Convert.ToString(Math.Floor(0.18 * (Math.Sqrt(Convert.ToInt32(NewXP))) + 1));
                    Lvl = ele.Attribute("Lvl").Value.ToString();
                }
                XElement users = XP.Element("users").Element(GetTopic(channel, user));
                users.SetAttributeValue("XP", NewXP);
                if (Convert.ToInt32(NewLvl) > Convert.ToInt32(Lvl))
                {
                    users.SetAttributeValue("Lvl", NewLvl);
                }
                XP.Save(@"Data\XP.xml");
                Thread.Sleep(10);
            }
        }

        public static void AddEnergy(string channel, string user)
        {
            CheckNoXP(channel, user);
            lock (_lockxp)
            {
                XDocument XP = XDocument.Load(@"Data\XP.xml");
                string MaxEnergy = null;
                string Energy = null;
                string Lvl = null;
                foreach (XElement ele in XP.Descendants(GetTopic(channel, user)))
                {
                    try
                    {
                        MaxEnergy = ele.Attribute("MaxEnergy").Value.ToString();
                    }
                    catch
                    {

                    }
                    Energy = ele.Attribute("Energy").Value.ToString();
                    Lvl = ele.Attribute("Lvl").Value.ToString();
                }
                XElement users = XP.Element("users").Element(GetTopic(channel, user));
                if (MaxEnergy == null)
                {
                    if (Convert.ToInt32(Lvl) >= 50)
                    {
                        users.Add(new XAttribute("MaxEnergy", "4"));
                        MaxEnergy = "4";
                    }
                    else
                    {
                        users.Add(new XAttribute("MaxEnergy", "3"));
                        MaxEnergy = "3";
                    }
                }
                if (Convert.ToInt32(Energy) < Convert.ToInt32(MaxEnergy))
                {
                    users.SetAttributeValue("Energy", Convert.ToString(Convert.ToInt32(Energy) + 1));
                }
                XP.Save(@"Data\XP.xml");
            }
        }

        private void ResetEnergy(string channel)
        {
            lock (_lockxp)
            {
                XDocument AddEnergy = XDocument.Load(@"Data\XP.xml");
                string MaxEnergy = null;
                string Lvl = null;
                foreach (XElement ele in AddEnergy.Element("users").Elements())
                {
                    MaxEnergy = null;
                    if (ele.Name.ToString().Substring(0, ele.Name.ToString().IndexOf('.')) == channel)
                    {
                        XElement users = AddEnergy.Element("users").Element(GetTopic(channel, ele.Name.ToString().Substring(ele.Name.ToString().IndexOf('.') + 1)));
                        try
                        {
                            MaxEnergy = users.Attribute("MaxEnergy").Value.ToString();
                        }
                        catch
                        {
                            continue;
                        }
                        Lvl = ele.Attribute("Lvl").Value.ToString();
                        if (MaxEnergy == null)
                        {
                            if (Convert.ToInt32(Lvl) >= 50)
                            {
                                users.Add(new XAttribute("MaxEnergy", "4"));
                                MaxEnergy = "4";
                            }
                            else
                            {
                                users.Add(new XAttribute("MaxEnergy", "3"));
                                MaxEnergy = "3";
                            }
                        }
                        users.SetAttributeValue("Energy", MaxEnergy);
                    }
                }
                AddEnergy.Save(@"Data\XP.xml");
            }
        }

        private void SmeltShards(string channel, string user, string type, int qty)
        {

            rift.CheckMatsUser(channel, user);
            lock (_lockloot)
            {
                XDocument loot = XDocument.Load(@"Data\Loot.xml");
                XElement P1 = loot.Element("loot").Element(GetTopic(channel, user)).Element("Materials");
                int Cur = 0;
                try
                {
                    Cur = Convert.ToInt32(P1.Attribute(type).Value);
                }
                catch
                {
                }
                P1.SetAttributeValue(type, Cur + qty);
                loot.Save(@"Data\Loot.xml");
            }
        }

        private void AdvStart(string channel, string user)
        {
            lock (_lockadv)
            {
                XDocument Adv2 = XDocument.Load(@"Data\Adventure.xml");
                XElement A2 = Adv2.Element("adventure").Element("Channel." + channel);
                A2.SetAttributeValue("Status", "On");
                Adv2.Save(@"Data\Adventure.xml");
            }
            client.SendMessage(channel, "/me : " + user.ToLower() + " is starting an adventure, to join them type !advjoin...");
            System.Threading.Thread.Sleep(10000);
            client.SendMessage(channel, "/me : 20 seconds until start...");
            System.Threading.Thread.Sleep(10000);
            client.SendMessage(channel, "/me : 10 seconds until start...");
            System.Threading.Thread.Sleep(5000);
            client.SendMessage(channel, "/me : 5 seconds until start...");
            System.Threading.Thread.Sleep(5000);
            client.SendMessage(channel, "/me : Adventure Starting...");
            System.Threading.Thread.Sleep(3000);
            adventure.StartAdventure(channel, user);
        }

        private void RaidStart(string channel, string user, int level)
        {
            lock (_lockadv)
            {
                XDocument Adv2 = XDocument.Load(@"Data\Adventure.xml");
                XElement A2 = Adv2.Element("adventure").Element("Channel." + channel);
                A2.SetAttributeValue("Status", "On");
                Adv2.Save(@"Data\Adventure.xml");
            }
            client.SendMessage(channel, "/me : " + user.ToLower() + " is starting a lvl (" + level + ") RAID, to join them type !raidjoin...");
            System.Threading.Thread.Sleep(15000);
            client.SendMessage(channel, "/me : 45 seconds until start...");
            System.Threading.Thread.Sleep(15000);
            client.SendMessage(channel, "/me : 30 seconds until start...");
            System.Threading.Thread.Sleep(10000);
            client.SendMessage(channel, "/me : 20 seconds until start...");
            System.Threading.Thread.Sleep(10000);
            client.SendMessage(channel, "/me : 10 seconds until start...");
            System.Threading.Thread.Sleep(10000);
            client.SendMessage(channel, "/me : Raid Starting...");
            System.Threading.Thread.Sleep(3000);
            adventure.StartRaid(channel, level);
        }

        private void AdvHard(string channel, string user)
        {
            lock (_lockadv)
            {
                XDocument Adv2 = XDocument.Load(@"Data\Adventure.xml");
                XElement A2 = Adv2.Element("adventure").Element("Channel." + channel);
                A2.SetAttributeValue("Status", "On");
                A2.SetAttributeValue("Difficulty", "Hard");
                Adv2.Save(@"Data\Adventure.xml");
            }
            client.SendMessage(channel, "/me : " + user.ToLower() + " is starting a HARD adventure, to join them type !advjoin...");
            System.Threading.Thread.Sleep(10000);
            client.SendMessage(channel, "/me : 20 seconds until start...");
            System.Threading.Thread.Sleep(10000);
            client.SendMessage(channel, "/me : 10 seconds until start...");
            System.Threading.Thread.Sleep(5000);
            client.SendMessage(channel, "/me : 5 seconds until start...");
            System.Threading.Thread.Sleep(5000);
            client.SendMessage(channel, "/me : Adventure Starting...");
            System.Threading.Thread.Sleep(3000);
            adventure.StartAdventure(channel, user);
        }

        private void Adv150(string channel, string user)
        {
            lock (_lockadv)
            {
                XDocument Adv2 = XDocument.Load(@"Data\Adventure.xml");
                XElement A2 = Adv2.Element("adventure").Element("Channel." + channel);
                A2.SetAttributeValue("Status", "On");
                A2.SetAttributeValue("Difficulty", "150");
                Adv2.Save(@"Data\Adventure.xml");
            }
            client.SendMessage(channel, "/me : " + user.ToLower() + " is starting a LVL 150 adventure, to join them type !advjoin...");
            System.Threading.Thread.Sleep(10000);
            client.SendMessage(channel, "/me : 20 seconds until start...");
            System.Threading.Thread.Sleep(10000);
            client.SendMessage(channel, "/me : 10 seconds until start...");
            System.Threading.Thread.Sleep(5000);
            client.SendMessage(channel, "/me : 5 seconds until start...");
            System.Threading.Thread.Sleep(5000);
            client.SendMessage(channel, "/me : Adventure Starting...");
            System.Threading.Thread.Sleep(3000);
            adventure.StartAdventure(channel, user);
        }

        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            if (e.ChatMessage.Message.StartsWith("!"))
            {
                string channel = e.ChatMessage.Channel.ToUpper();
                string[] args = e.ChatMessage.Message.ToUpper().Split(' ');
                Thread chat = new Thread(() => ChatCommands(e, channel, args));
                chat.Start();
            }
        }

        private void ChatCommands(OnMessageReceivedArgs e, string channel, string[] args)
        {
            CheckNoXP(channel, e.ChatMessage.Username.ToUpper());
                if (args[0] == "!RESETENERGY")
                {
                    if (e.ChatMessage.Username.ToUpper() == "T3MPU5_FU91T_")
                    {
                        ResetEnergy(channel);
                        client.SendMessage(channel, "/me : Reset all energy!");
                    }
                }
                if (args[0] == "!ADDSTATS")
                {
                    if (e.ChatMessage.Username.ToUpper() == "T3MPU5_FU91T_")
                    {
                        AddNewStats();
                        client.SendMessage(channel, "/me : Added the new stats.");
                    }
                }
                //      if (args[0] == "!CHEST")
                //      {
                //          client.SendWhisper(e.ChatMessage.Username, @"         __________ /n
                //  /\____; ; ___\ /n
                // | /         / /n
                // `. ())oo(). /n
                //  |\(% () * ^^() ^\ /n
                // %| | -% -------| /n
                //% \ | %  ))   | /n
                //%  \|% ________ |");
                //      }
                if (args[0] == "!TESTLOOT")
                {
                    client.SendMessage(channel, "/me : Created loot: " + adventure.LootTest());
                }
                if (args[0] == "!GLOBAL")
                {
                    if (args.Length < 2)
                    {
                        if (GetTopic(channel, e.ChatMessage.Username.ToUpper()).Contains("Global"))
                        {
                            client.SendMessage(channel, "/me : This channel is opted into Global stats.  You can opt out with !global optout.");
                        }
                        else
                        {
                            client.SendMessage(channel, "/me : This channel is opted out of Global stats.  You can opt in with !global optin.");
                        }
                    }
                    else
                    {
                        lock (_lockchann)
                        {
                            XDocument Chan = XDocument.Load(@"Data\Channels.xml");
                            var ele = Chan.Element("channels").Elements("Channel." + channel).SingleOrDefault();
                            if (args[1] == "OPTIN" && channel.Contains(e.ChatMessage.Username.ToUpper()))
                            {
                                if (ele == null)
                                {
                                    Chan.Element("channels").Add(new XElement("Channel." + channel));
                                    Chan.Element("channels").Element("Channel." + channel).SetAttributeValue("Global", "Yes");
                                    Chan.Save(@"Data\Channels.xml");
                                    client.SendMessage(channel, "/me : This channel is now opted into Global stats.");
                                }
                                else
                                {
                                    ele.SetAttributeValue("Global", "Yes");
                                    Chan.Save(@"Data\Channels.xml");
                                    client.SendMessage(channel, "/me : This channel is now opted into Global stats.");
                                }
                            }
                            else if (args[1] == "OPTOUT" && channel.Contains(e.ChatMessage.Username.ToUpper()))
                            {
                                if (ele == null)
                                {
                                    Chan.Element("channels").Add(new XElement("Channel." + channel));
                                    Chan.Element("channels").Element("Channel." + channel).SetAttributeValue("Global", null);
                                    Chan.Save(@"Data\Channels.xml");
                                    client.SendMessage(channel, "/me : This channel is now opted out of Global stats.");
                                }
                                else
                                {
                                    ele.SetAttributeValue("Global", null);
                                    Chan.Save(@"Data\Channels.xml");
                                    client.SendMessage(channel, "/me : This channel is now opted out of Global stats.");
                                }
                            }
                            else
                            {
                                client.SendMessage(channel, "/me : You are not permitted to use this command. Channel owner must opt in/out of Global Stats!");
                            }
                        }
                    }
                }
                if (args[0] == "!ADVTOP")
                {
                    if (args.Length == 1)
                    {
                        var top10 = LeaderboardXP(channel).Take(10);
                        string msg = null;
                        int Pos = 1;
                        foreach (var User in top10)
                        {
                            msg = msg + Pos + ". " + User.User.ToLower() + ": (" + User.XP + ") XP, lvl (" + LookUpLvl(channel, User.User) + ")  ";
                            Pos++;
                        }
                        client.SendMessage(channel, "/me : " + msg);
                    }
                    else if (args[1] == "STR")
                    {
                        var top10 = LeaderboardStr(channel).Take(10);
                        string msg = null;
                        int Pos = 1;
                        foreach (var User in top10)
                        {
                            msg = msg + Pos + ". " + User.User.ToLower() + ": Strength (" + User.STR + ") ";
                            Pos++;
                        }
                        client.SendMessage(channel, "/me : " + msg);
                    }
                    else if (args[1] == "END")
                    {
                        var top10 = LeaderboardEnd(channel).Take(10);
                        string msg = null;
                        int Pos = 1;
                        foreach (var User in top10)
                        {
                            msg = msg + Pos + ". " + User.User.ToLower() + ": Endurance (" + User.END + ") ";
                            Pos++;
                        }
                        client.SendMessage(channel, "/me : " + msg);
                    }
                    else if (args[1] == "INT")
                    {
                        var top10 = LeaderboardInt(channel).Take(10);
                        string msg = null;
                        int Pos = 1;
                        foreach (var User in top10)
                        {
                            msg = msg + Pos + ". " + User.User.ToLower() + ": Intelligence (" + User.INT + ") ";
                            Pos++;
                        }
                        client.SendMessage(channel, "/me : " + msg);
                    }
                    else if (args[1] == "DEX")
                    {
                        var top10 = LeaderboardDex(channel).Take(10);
                        string msg = null;
                        int Pos = 1;
                        foreach (var User in top10)
                        {
                            msg = msg + Pos + ". " + User.User.ToLower() + ": Dexterity (" + User.DEX + ") ";
                            Pos++;
                        }
                        client.SendMessage(channel, "/me : " + msg);
                    }
                }
                if (args[0] == "!EQUIPPED")
                {
                    CheckNoXP(channel, e.ChatMessage.Username.ToUpper());
                    string sword = adventure.GetWeapon(channel, e.ChatMessage.Username.ToUpper());
                    string helm = adventure.GetHelm(channel, e.ChatMessage.Username.ToUpper());
                    string chest = adventure.GetChest(channel, e.ChatMessage.Username.ToUpper());
                    string legs = adventure.GetLegs(channel, e.ChatMessage.Username.ToUpper());
                    string boots = adventure.GetBoots(channel, e.ChatMessage.Username.ToUpper());
                    string ring = adventure.GetRing(channel, e.ChatMessage.Username.ToUpper());
                    if (sword.ToUpper().Contains("HANDS"))
                    {
                        sword = "(empty)";
                    }
                    if (helm.ToUpper().Contains("EMPTY"))
                    {
                        helm = "(empty)";
                    }
                    if (chest.ToUpper().Contains("EMPTY"))
                    {
                        chest = "(empty)";
                    }
                    if (legs.ToUpper().Contains("EMPTY"))
                    {
                        legs = "(empty)";
                    }
                    if (boots.ToUpper().Contains("EMPTY"))
                    {
                        boots = "(empty)";
                    }
                    if (ring.ToUpper().Contains("EMPTY"))
                    {
                        ring = "(empty)";
                    }
                    client.SendMessage(channel, "/me : Your Equipment: [Weapon] - " + sword + ", [Ring] - " + ring + ", [Helm] - " + helm + ", [Chest] - " + chest + ", [Legs] - " + legs + ", [Boots] - " + boots + ".");
                }
                if (args[0] == "!RING")
                {
                    //if (args.Length == 2 && args[1] == "RESET")
                    //{
                    //        XDocument Loot = XDocument.Load(@"Data\Loot.xml");
                    //        XElement P = Loot.Element("loot");
                    //        foreach (XElement ele in P.Elements())
                    //        {
                    //            if (ele.Name.ToString().StartsWith("MEELUX."))
                    //            {
                    //                string user = ele.Name.ToString().Substring(7);
                    //                adventure.CheckLootUser(channel, user);
                    //                XElement P2 = P.Element("MEELUX." + user).Element("rings");
                    //                if (adventure.GetLootSlot1(channel, user) != null && adventure.GetLootSlot1(channel, user).Contains("Ring"))
                    //                {
                    //                    string loot = adventure.GetLootSlot1(channel, user);
                    //                    P2.SetAttributeValue(adventure.GetNextRingLootSlot(channel, user), loot);
                    //                    ele.SetAttributeValue("Slot1", null);
                    //                }
                    //                if (adventure.GetLootSlot2(channel, user) != null && adventure.GetLootSlot2(channel, user).Contains("Ring"))
                    //                {
                    //                    string loot = adventure.GetLootSlot2(channel, user);
                    //                    P2.SetAttributeValue(adventure.GetNextRingLootSlot(channel, user), loot);
                    //                    ele.SetAttributeValue("Slot2", null);
                    //                }
                    //                if (adventure.GetLootSlot3(channel, user) != null && adventure.GetLootSlot3(channel, user).Contains("Ring"))
                    //                {
                    //                    string loot = adventure.GetLootSlot3(channel, user);
                    //                    P2.SetAttributeValue(adventure.GetNextRingLootSlot(channel, user), loot);
                    //                    ele.SetAttributeValue("Slot3", null);
                    //                }
                    //                if (adventure.GetLootSlot4(channel, user) != null && adventure.GetLootSlot4(channel, user).Contains("Ring"))
                    //                {
                    //                    string loot = adventure.GetLootSlot4(channel, user);
                    //                    P2.SetAttributeValue(adventure.GetNextRingLootSlot(channel, user), loot);
                    //                    ele.SetAttributeValue("Slot4", null);
                    //                }
                    //                if (adventure.GetLootSlot5(channel, user) != null && adventure.GetLootSlot5(channel, user).Contains("Ring"))
                    //                {
                    //                    string loot = adventure.GetLootSlot5(channel, user);
                    //                    P2.SetAttributeValue(adventure.GetNextRingLootSlot(channel, user), loot);
                    //                    ele.SetAttributeValue("Slot5", null);
                    //                }
                    //            }
                    //        }
                    //        Loot.Save(@"Data\Loot.xml");
                    //}
                    if (args.Length == 2 && args[1] == "BAG")
                    {
                        CheckNoXP(channel, e.ChatMessage.Username.ToUpper());
                        string Slot1 = adventure.GetRingLootSlot1(channel, e.ChatMessage.Username.ToUpper());
                        string Slot2 = adventure.GetRingLootSlot2(channel, e.ChatMessage.Username.ToUpper());
                        string Slot3 = adventure.GetRingLootSlot3(channel, e.ChatMessage.Username.ToUpper());
                        string Slot4 = adventure.GetRingLootSlot4(channel, e.ChatMessage.Username.ToUpper());
                        string Slot5 = adventure.GetRingLootSlot5(channel, e.ChatMessage.Username.ToUpper());
                        string Slot6 = adventure.GetRingLootSlot6(channel, e.ChatMessage.Username.ToUpper());
                        if (Slot1 == null)
                        {
                            Slot1 = "(empty)";
                        }
                        if (Slot2 == null)
                        {
                            Slot2 = "(empty)";
                        }
                        if (Slot3 == null)
                        {
                            Slot3 = "(empty)";
                        }
                        if (Slot4 == null)
                        {
                            Slot4 = "(empty)";
                        }
                        if (Slot5 == null)
                        {
                            Slot5 = "(empty)";
                        }
                        if (Slot6 == null)
                        {
                            Slot6 = "(empty)";
                        }
                        client.SendMessage(channel, "/me : Your ring bag has: [1] - " + Slot1 + ", [2] - " + Slot2 + ", [3] - " + Slot3 + ", [4] - " + Slot4 + ", [5] - " + Slot5 + ", [6] - " + Slot6 + ".");
                    }
                    else if (args.Length > 1 && args[1] == "EQUIP")
                    {
                        if (args.Length < 3)
                        {
                            client.SendMessage(channel, "/me : Usage: !ring equip [bag slot #]");
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(args[2]) && !args[2].All(Char.IsDigit))
                            {
                                client.SendMessage(channel, "/me : This is not valid, stop trying to break me!");
                            }
                            else if (Enumerable.Range(1, 6).Contains(Convert.ToInt32(args[2])))
                            {
                                string loot = null;
                                string slot = null;
                                if (args[2] == "1")
                                {
                                    loot = adventure.GetRingLootSlot1(channel, e.ChatMessage.Username.ToUpper());
                                    slot = "Slot1";
                                }
                                if (args[2] == "2")
                                {
                                    loot = adventure.GetRingLootSlot2(channel, e.ChatMessage.Username.ToUpper());
                                    slot = "Slot2";
                                }
                                if (args[2] == "3")
                                {
                                    loot = adventure.GetRingLootSlot3(channel, e.ChatMessage.Username.ToUpper());
                                    slot = "Slot3";
                                }
                                if (args[2] == "4")
                                {
                                    loot = adventure.GetRingLootSlot4(channel, e.ChatMessage.Username.ToUpper());
                                    slot = "Slot4";
                                }
                                if (args[2] == "5")
                                {
                                    loot = adventure.GetRingLootSlot5(channel, e.ChatMessage.Username.ToUpper());
                                    slot = "Slot5";
                                }
                                if (args[2] == "6")
                                {
                                    loot = adventure.GetRingLootSlot6(channel, e.ChatMessage.Username.ToUpper());
                                    slot = "Slot6";
                                }
                                if (loot == null)
                                {
                                    client.SendMessage(channel, "/me : " + e.ChatMessage.DisplayName + " you have no loot in this slot.");
                                }
                                else
                                {
                                    adventure.CheckLootUser(channel, e.ChatMessage.Username.ToUpper());
                                    lock (_lockloot)
                                    {
                                        XDocument Loot = XDocument.Load(@"Data\Loot.xml");
                                        XElement L = Loot.Element("loot").Element(GetTopic(channel, e.ChatMessage.Username.ToUpper()));
                                        string oldSlot = null;
                                        try
                                        {
                                            oldSlot = L.Attribute("Ring").Value;
                                        }
                                        catch
                                        {

                                        }
                                        if (oldSlot != null)
                                        {
                                            client.SendMessage(channel, "/me : " + e.ChatMessage.DisplayName + " swapped " + oldSlot + " with " + loot + ".");
                                        }
                                        else
                                        {
                                            client.SendMessage(channel, "/me : " + e.ChatMessage.DisplayName + " equipped " + loot + ".");
                                        }
                                        L.SetAttributeValue("Ring", loot);
                                        L.Element("rings").SetAttributeValue(slot, oldSlot);
                                        Loot.Save(@"Data\Loot.xml");
                                    }
                                }
                            }
                        }
                    }
                    else if (args.Length > 1 && args[1] == "THROW")
                    {
                        if (args.Length < 3)
                        {
                            client.SendMessage(channel, "/me : Please pick an item slot to throw away.");
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(args[2]) && !args[2].All(Char.IsDigit))
                            {
                                client.SendMessage(channel, "/me : This is not valid, stop trying to break me!");
                            }
                            else if (Enumerable.Range(1, 6).Contains(Convert.ToInt32(args[2])))
                            {
                                string loot = null;
                                if (args[2] == "1")
                                {
                                    loot = adventure.GetRingLootSlot1(channel, e.ChatMessage.Username.ToUpper());
                                }
                                if (args[2] == "2")
                                {
                                    loot = adventure.GetRingLootSlot2(channel, e.ChatMessage.Username.ToUpper());
                                }
                                if (args[2] == "3")
                                {
                                    loot = adventure.GetRingLootSlot3(channel, e.ChatMessage.Username.ToUpper());
                                }
                                if (args[2] == "4")
                                {
                                    loot = adventure.GetRingLootSlot4(channel, e.ChatMessage.Username.ToUpper());
                                }
                                if (args[2] == "5")
                                {
                                    loot = adventure.GetRingLootSlot5(channel, e.ChatMessage.Username.ToUpper());
                                }
                                if (args[2] == "6")
                                {
                                    loot = adventure.GetRingLootSlot6(channel, e.ChatMessage.Username.ToUpper());
                                }
                                client.SendMessage(channel, "/me : " + e.ChatMessage.DisplayName + " threw away " + loot);
                                adventure.CheckLootUser(channel, e.ChatMessage.Username.ToUpper());
                                lock (_lockloot)
                                {
                                    XDocument Loot = XDocument.Load(@"Data\Loot.xml");
                                    XElement L = Loot.Element("loot").Element(GetTopic(channel, e.ChatMessage.Username.ToUpper())).Element("rings");
                                    L.SetAttributeValue("Slot" + args[2], null);
                                    Loot.Save(@"Data\Loot.xml");
                                }
                                PickUp = true;
                                PickUpLoot = loot;
                                Thread.Sleep(2000);
                                PickUp = false;
                                PickUpLoot = null;
                            }
                            else
                            {
                                client.SendMessage(channel, "/me : You only have 6 ring slots, pick a slot 1 to 6.");
                            }
                        }
                    }
                }
                if (args[0] == "!LOOT" || args[0] == "!BAG")
                {
                    CheckNoXP(channel, e.ChatMessage.Username.ToUpper());
                    string Slot1 = adventure.GetLootSlot1(channel, e.ChatMessage.Username.ToUpper());
                    string Slot2 = adventure.GetLootSlot2(channel, e.ChatMessage.Username.ToUpper());
                    string Slot3 = adventure.GetLootSlot3(channel, e.ChatMessage.Username.ToUpper());
                    string Slot4 = adventure.GetLootSlot4(channel, e.ChatMessage.Username.ToUpper());
                    string Slot5 = adventure.GetLootSlot5(channel, e.ChatMessage.Username.ToUpper());
                    if (Slot1 == null)
                    {
                        Slot1 = "(empty)";
                    }
                    if (Slot2 == null)
                    {
                        Slot2 = "(empty)";
                    }
                    if (Slot3 == null)
                    {
                        Slot3 = "(empty)";
                    }
                    if (Slot4 == null)
                    {
                        Slot4 = "(empty)";
                    }
                    if (Slot5 == null)
                    {
                        Slot5 = "(empty)";
                    }
                    string msg = "/me : Your loot bag has: [1] - " + Slot1 + ", [2] - " + Slot2 + ", [3] - " + Slot3 + ", [4] - " + Slot4 + ", [5] - " + Slot5;
                    if (shop.GetMaxBagSlots(channel, e.ChatMessage.Username.ToUpper()) > 5)
                    {
                        int s = 6;
                        while (s <= shop.GetMaxBagSlots(channel, e.ChatMessage.Username.ToUpper()))
                        {
                            string SlotN = adventure.GetLootSlotN(channel, e.ChatMessage.Username.ToUpper(), s);
                            if (SlotN == null)
                            {
                                SlotN = "(empty)";
                            }
                            msg = msg + ", [" + s + "] - " + SlotN;
                            s++;
                        }
                    }
                    client.SendMessage(channel, msg + ".");
                }
                if (args[0] == "!SHARDS")
                {
                    CheckNoXP(channel, e.ChatMessage.Username.ToUpper());
                    int Steel = rift.GetSteelShards(channel, e.ChatMessage.Username.ToUpper());
                    int Moonstone = rift.GetMoonShards(channel, e.ChatMessage.Username.ToUpper());
                    int Fire = rift.GetFireShards(channel, e.ChatMessage.Username.ToUpper());
                    int Enchanted = rift.GetEnchShards(channel, e.ChatMessage.Username.ToUpper());
                    string msg = "/me : Your shard bag has: [Steel] - " + Steel + " Shards, [Moonstone] - " + Moonstone + " Shards, [Fire] - " + Fire + " Shards, [Enchanted] - " + Enchanted + " Shards";
                    int Vibranium = 0;
                    int Infinity = 0;

                    try
                    {
                        Vibranium = rift.GetVibShards(channel, e.ChatMessage.Username.ToUpper());
                    }
                    catch
                    {

                    }
                    try
                    {
                        Infinity = rift.GetInfShards(channel, e.ChatMessage.Username.ToUpper());
                    }
                    catch
                    {

                    }
                    if (Vibranium > 0)
                    {
                        msg = msg + ", [Vibranium] - " + Vibranium + " Shards";
                    }
                    if (Infinity > 0)
                    {
                        msg = msg + ", [Infinity] - " + Infinity + " Shards";
                    }
                    client.SendMessage(channel, msg + ".");
                }
                if (args[0] == "!SMELT")
                {
                    CheckNoXP(channel, e.ChatMessage.Username.ToUpper());
                    if (args.Length < 2)
                    {
                        client.SendMessage(channel, "/me : Please pick an item to smelt.");
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(args[1]) && args[1].All(Char.IsDigit) && Enumerable.Range(1, shop.GetMaxBagSlots(channel, e.ChatMessage.Username.ToUpper())).Contains(Convert.ToInt32(args[1])))
                        {
                            string loot = null;
                            loot = adventure.GetLootSlotN(channel, e.ChatMessage.Username.ToUpper(), Convert.ToInt32(args[1]));
                            if (loot == null)
                            {
                                client.SendMessage(channel, "/me : There is nothing to smelt!");
                            }
                            else if (loot.Contains("Coins"))
                            {
                                client.SendMessage(channel, "/me : You cannot smelt Gold Coins!");
                            }
                            else if (loot.Contains("Wooden"))
                            {
                                client.SendMessage(channel, "/me : You cannot smelt Wooden Items!");
                            }
                            else if (loot.Split(' ').Length < 3)
                            {
                                client.SendMessage(channel, "/me : You cannot smelt this " + loot + ".");
                            }
                            else
                            {
                                int qty = 0;
                                if (loot.Contains("Eridin"))
                                {
                                    qty = random.Next(1, 4);
                                }
                                else if (loot.Contains("Brahma"))
                                {
                                    qty = random.Next(3, 6);
                                }
                                else if (loot.Contains("Night"))
                                {
                                    qty = random.Next(5, 8);
                                }
                                else if (loot.Contains("Destiny"))
                                {
                                    qty = random.Next(7, 11);
                                }
                                SmeltShards(channel, e.ChatMessage.Username.ToUpper(), loot.Split(' ')[0], qty);
                                client.SendMessage(channel, "/me : You smelted your " + loot + " into " + qty + " " + loot.Split(' ')[0] + " shards!");
                                lock (_lockloot)
                                {
                                    XDocument Loot = XDocument.Load(@"Data\Loot.xml");
                                    XElement L = Loot.Element("loot").Element(GetTopic(channel, e.ChatMessage.Username.ToUpper()));
                                    L.SetAttributeValue("Slot" + args[1], null);
                                    Loot.Save(@"Data\Loot.xml");
                                }
                            }
                        }
                        else
                        {
                            int qty = 25;
                            if (args.Length > 2 && args[2].All(Char.IsDigit))
                            {
                                if ((Convert.ToInt32(args[2]) % 25) == 0)
                                {
                                    qty = Convert.ToInt32(args[2]);
                                }
                                else
                                {
                                    client.SendMessage(channel, "/me : Qty must be in a multiple of 25!!");
                                    return;
                                }
                            }
                            if (args[1].ToUpper() == "STEEL")
                            {
                                lock (_lockloot)
                                {
                                    XDocument loot = XDocument.Load(@"Data\Loot.xml");
                                    XElement P1 = loot.Element("loot").Element(GetTopic(channel, e.ChatMessage.Username.ToUpper())).Element("Materials");
                                    int Cur = 0;
                                    int Cur2 = 0;
                                    try
                                    {
                                        Cur = Convert.ToInt32(P1.Attribute("Steel").Value);
                                    }
                                    catch
                                    {
                                    }
                                    if (Cur >= qty)
                                    {
                                        try
                                        {
                                            Cur2 = Convert.ToInt32(P1.Attribute("Moonstone").Value);
                                        }
                                        catch
                                        {
                                        }
                                        P1.SetAttributeValue("Steel", Cur - qty);
                                        P1.SetAttributeValue("Moonstone", Cur2 + (qty / 25));
                                        loot.Save(@"Data\Loot.xml");
                                        client.SendMessage(channel, "/me : You smelted " + qty + " Steel Shards into " + qty / 25 + " Moonstone Shards!");
                                    }
                                    else
                                    {
                                        client.SendMessage(channel, "/me : You don't have enough Steel Shards.");
                                    }
                                }
                            }
                            else if (args[1].ToUpper() == "MOONSTONE")
                            {
                                lock (_lockloot)
                                {
                                    XDocument loot = XDocument.Load(@"Data\Loot.xml");
                                    XElement P1 = loot.Element("loot").Element(GetTopic(channel, e.ChatMessage.Username.ToUpper())).Element("Materials");
                                    int Cur = 0;
                                    int Cur2 = 0;
                                    try
                                    {
                                        Cur = Convert.ToInt32(P1.Attribute("Moonstone").Value);
                                    }
                                    catch
                                    {
                                    }
                                    if (Cur >= qty)
                                    {
                                        try
                                        {
                                            Cur2 = Convert.ToInt32(P1.Attribute("Fire").Value);
                                        }
                                        catch
                                        {
                                        }
                                        P1.SetAttributeValue("Moonstone", Cur - qty);
                                        P1.SetAttributeValue("Fire", Cur2 + (qty / 25));
                                        loot.Save(@"Data\Loot.xml");
                                        client.SendMessage(channel, "/me : You smelted " + qty + " Moonstone Shards into " + qty / 25 + " Fire Shards!");
                                    }
                                    else
                                    {
                                        client.SendMessage(channel, "/me : You don't have enough Moonstone Shards.");
                                    }
                                }
                            }
                            else if (args[1].ToUpper() == "FIRE")
                            {
                                lock (_lockloot)
                                {
                                    XDocument loot = XDocument.Load(@"Data\Loot.xml");
                                    XElement P1 = loot.Element("loot").Element(GetTopic(channel, e.ChatMessage.Username.ToUpper())).Element("Materials");
                                    int Cur = 0;
                                    int Cur2 = 0;
                                    try
                                    {
                                        Cur = Convert.ToInt32(P1.Attribute("Fire").Value);
                                    }
                                    catch
                                    {
                                    }
                                    if (Cur >= qty)
                                    {
                                        try
                                        {
                                            Cur2 = Convert.ToInt32(P1.Attribute("Enchanted").Value);
                                        }
                                        catch
                                        {
                                        }
                                        P1.SetAttributeValue("Fire", Cur - qty);
                                        P1.SetAttributeValue("Enchanted", Cur2 + (qty / 25));
                                        loot.Save(@"Data\Loot.xml");
                                        client.SendMessage(channel, "/me : You smelted " + qty + " Fire Shards into " + qty / 25 + " Enchanted Shards!");
                                    }
                                    else
                                    {
                                        client.SendMessage(channel, "/me : You don't have enough Fire Shards.");
                                    }
                                }
                            }
                            else if (args[1].ToUpper() == "ENCHANTED")
                            {
                                lock (_lockloot)
                                {
                                    XDocument loot = XDocument.Load(@"Data\Loot.xml");
                                    XElement P1 = loot.Element("loot").Element(GetTopic(channel, e.ChatMessage.Username.ToUpper())).Element("Materials");
                                    int Cur = 0;
                                    int Cur2 = 0;
                                    try
                                    {
                                        Cur = Convert.ToInt32(P1.Attribute("Enchanted").Value);
                                    }
                                    catch
                                    {
                                    }
                                    if (Cur >= qty)
                                    {
                                        try
                                        {
                                            Cur2 = Convert.ToInt32(P1.Attribute("Vibranium").Value);
                                        }
                                        catch
                                        {
                                        }
                                        P1.SetAttributeValue("Enchanted", Cur - qty);
                                        P1.SetAttributeValue("Vibranium", Cur2 + (qty / 25));
                                        loot.Save(@"Data\Loot.xml");
                                        client.SendMessage(channel, "/me : You smelted " + qty + " Enchanted Shards into " + qty / 25 + " Vibranium Shards!");
                                    }
                                    else
                                    {
                                        client.SendMessage(channel, "/me : You don't have enough Enchanted Shards.");
                                    }
                                }
                            }
                            else if (args[1].ToUpper() == "VIBRANIUM")
                            {
                                lock (_lockloot)
                                {
                                    XDocument loot = XDocument.Load(@"Data\Loot.xml");
                                    XElement P1 = loot.Element("loot").Element(GetTopic(channel, e.ChatMessage.Username.ToUpper())).Element("Materials");
                                    int Cur = 0;
                                    int Cur2 = 0;
                                    try
                                    {
                                        Cur = Convert.ToInt32(P1.Attribute("Vibranium").Value);
                                    }
                                    catch
                                    {
                                    }
                                    if (Cur >= qty)
                                    {
                                        try
                                        {
                                            Cur2 = Convert.ToInt32(P1.Attribute("Infinity").Value);
                                        }
                                        catch
                                        {
                                        }
                                        P1.SetAttributeValue("Vibranium", Cur - qty);
                                        P1.SetAttributeValue("Infinity", Cur2 + (qty / 25));
                                        loot.Save(@"Data\Loot.xml");
                                        client.SendMessage(channel, "/me : You smelted " + qty + " Vibranium Shards into " + qty / 25 + " Infinity Shards!");
                                    }
                                    else
                                    {
                                        client.SendMessage(channel, "/me : You don't have enough Vibranium Shards.");
                                    }
                                }
                            }
                        }
                    }
                }
                if (args[0] == "!THROW")
                {
                    CheckNoXP(channel, e.ChatMessage.Username.ToUpper());
                    if (args.Length < 2)
                    {
                        client.SendMessage(channel, "/me : Please pick an item slot to throw away.");
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(args[1]) && !args[1].All(Char.IsDigit))
                        {
                            client.SendMessage(channel, "/me : This is not valid, stop trying to break me!");
                        }
                        else if (Enumerable.Range(1, shop.GetMaxBagSlots(channel, e.ChatMessage.Username.ToUpper())).Contains(Convert.ToInt32(args[1])))
                        {
                            string loot = null;
                            loot = adventure.GetLootSlotN(channel, e.ChatMessage.Username.ToUpper(), Convert.ToInt32(args[1]));
                            client.SendMessage(channel, "/me : " + e.ChatMessage.DisplayName + " threw away " + loot);
                            adventure.CheckLootUser(channel, e.ChatMessage.Username.ToUpper());
                            lock (_lockloot)
                            {
                                XDocument Loot = XDocument.Load(@"Data\Loot.xml");
                                XElement L = Loot.Element("loot").Element(GetTopic(channel, e.ChatMessage.Username.ToUpper()));
                                L.SetAttributeValue("Slot" + args[1], null);
                                Loot.Save(@"Data\Loot.xml");
                            }
                            PickUp = true;
                            PickUpLoot = loot;
                            Thread.Sleep(2000);
                            PickUp = false;
                            PickUpLoot = null;
                        }
                        else
                        {
                            client.SendMessage(channel, "/me : You only have " + shop.GetMaxBagSlots(channel, e.ChatMessage.Username.ToUpper()) + " item slots, pick a slot 1 to " + shop.GetMaxBagSlots(channel, e.ChatMessage.Username.ToUpper()) + ".");
                        }
                    }
                }
                if (args[0] == "!PICKUP")
                {
                    if (PickUp)
                    {
                        if (!shop.IsBagFull(channel, e.ChatMessage.Username.ToUpper()))
                        {
                            PickUp = false;
                            lock (_lockloot)
                            {
                                XDocument Loot = XDocument.Load(@"Data\Loot.xml");
                                XElement L = Loot.Element("loot").Element(GetTopic(channel, e.ChatMessage.Username.ToUpper()));
                                L.SetAttributeValue(adventure.GetNextLootSlot(channel, e.ChatMessage.Username.ToUpper()), PickUpLoot);
                                client.SendMessage(channel, "/me : " + e.ChatMessage.Username + " picked up " + PickUpLoot + ".");
                                Loot.Save(@"Data\Loot.xml");
                            }
                            PickUpLoot = null;
                        }
                        else
                        {
                            client.SendMessage(channel, "/me : Your bag is full!");
                        }
                    }
                    else
                    {
                        client.SendMessage(channel, "/me : There is nothing to pick up!");
                    }
                }
                if (args[0] == "!EQUIP")
                {
                    CheckNoXP(channel, e.ChatMessage.Username.ToUpper());
                    if (args.Length < 2)
                    {
                        client.SendMessage(channel, "/me : Usage: !equip [bag slot #]");
                    }
                    else if (Enumerable.Range(1, shop.GetMaxBagSlots(channel, e.ChatMessage.Username.ToUpper())).Contains(Convert.ToInt32(args[1])))
                    {
                        string loot = null;
                        string slot = null;
                        loot = adventure.GetLootSlotN(channel, e.ChatMessage.Username.ToUpper(), Convert.ToInt32(args[1]));
                        slot = "Slot" + args[1];
                        if (loot == null)
                        {
                            client.SendMessage(channel, "/me : " + e.ChatMessage.DisplayName + " you have no loot in this slot.");
                        }
                        else
                        {
                            adventure.CheckLootUser(channel, e.ChatMessage.Username.ToUpper());
                            lock (_lockloot)
                            {
                                XDocument Loot = XDocument.Load(@"Data\Loot.xml");
                                XElement L = Loot.Element("loot").Element(GetTopic(channel, e.ChatMessage.Username.ToUpper()));
                                string oldSlot = null;
                                if (loot.Contains("Sword") || loot.Contains("Axe") || loot.Contains("Bow") || loot.Contains("Hammer"))
                                {
                                    try
                                    {
                                        oldSlot = L.Attribute("Sword").Value;
                                    }
                                    catch
                                    {

                                    }
                                    if (oldSlot != null)
                                    {
                                        client.SendMessage(channel, "/me : " + e.ChatMessage.DisplayName + " swapped " + oldSlot + " with " + loot + ".");
                                    }
                                    else
                                    {
                                        client.SendMessage(channel, "/me : " + e.ChatMessage.DisplayName + " equipped " + loot + ".");
                                    }
                                    L.SetAttributeValue("Sword", loot);
                                    L.SetAttributeValue(slot, oldSlot);
                                    Loot.Save(@"Data\Loot.xml");
                                }
                                else if (loot.Contains("Helm"))
                                {
                                    try
                                    {
                                        oldSlot = L.Attribute("Helm").Value;
                                    }
                                    catch
                                    {

                                    }
                                    if (oldSlot != null)
                                    {
                                        client.SendMessage(channel, "/me : " + e.ChatMessage.DisplayName + " swapped " + oldSlot + " with " + loot + ".");
                                    }
                                    else
                                    {
                                        client.SendMessage(channel, "/me : " + e.ChatMessage.DisplayName + " equipped " + loot + ".");
                                    }
                                    L.SetAttributeValue("Helm", loot);
                                    L.SetAttributeValue(slot, oldSlot);
                                    Loot.Save(@"Data\Loot.xml");
                                }
                                else if (loot.Contains("Chest"))
                                {
                                    try
                                    {
                                        oldSlot = L.Attribute("Chest").Value;
                                    }
                                    catch
                                    {

                                    }
                                    if (oldSlot != null)
                                    {
                                        client.SendMessage(channel, "/me : " + e.ChatMessage.DisplayName + " swapped " + oldSlot + " with " + loot + ".");
                                    }
                                    else
                                    {
                                        client.SendMessage(channel, "/me : " + e.ChatMessage.DisplayName + " equipped " + loot + ".");
                                    }
                                    L.SetAttributeValue("Chest", loot);
                                    L.SetAttributeValue(slot, oldSlot);
                                    Loot.Save(@"Data\Loot.xml");
                                }
                                else if (loot.Contains("Legs"))
                                {
                                    try
                                    {
                                        oldSlot = L.Attribute("Legs").Value;
                                    }
                                    catch
                                    {

                                    }
                                    if (oldSlot != null)
                                    {
                                        client.SendMessage(channel, "/me : " + e.ChatMessage.DisplayName + " swapped " + oldSlot + " with " + loot + ".");
                                    }
                                    else
                                    {
                                        client.SendMessage(channel, "/me : " + e.ChatMessage.DisplayName + " equipped " + loot + ".");
                                    }
                                    L.SetAttributeValue("Legs", loot);
                                    L.SetAttributeValue(slot, oldSlot);
                                    Loot.Save(@"Data\Loot.xml");
                                }
                                else if (loot.Contains("Boots"))
                                {
                                    try
                                    {
                                        oldSlot = L.Attribute("Boots").Value;
                                    }
                                    catch
                                    {

                                    }
                                    if (oldSlot != null)
                                    {
                                        client.SendMessage(channel, "/me : " + e.ChatMessage.DisplayName + " swapped " + oldSlot + " with " + loot + ".");
                                    }
                                    else
                                    {
                                        client.SendMessage(channel, "/me : " + e.ChatMessage.DisplayName + " equipped " + loot + ".");
                                    }
                                    L.SetAttributeValue("Boots", loot);
                                    L.SetAttributeValue(slot, oldSlot);
                                    Loot.Save(@"Data\Loot.xml");
                                }
                                else if (loot.Contains("Ring"))
                                {
                                    try
                                    {
                                        oldSlot = L.Attribute("Ring").Value;
                                    }
                                    catch
                                    {

                                    }
                                    if (oldSlot != null)
                                    {
                                        client.SendMessage(channel, "/me : " + e.ChatMessage.DisplayName + " swapped " + oldSlot + " with " + loot + ".");
                                    }
                                    else
                                    {
                                        client.SendMessage(channel, "/me : " + e.ChatMessage.DisplayName + " equipped " + loot + ".");
                                    }
                                    L.SetAttributeValue("Ring", loot);
                                    L.SetAttributeValue(slot, oldSlot);
                                    Loot.Save(@"Data\Loot.xml");
                                }
                                else if (loot.Contains("Coins"))
                                {
                                    client.SendMessage(channel, "/me : You cannot equip Gold Coins!");
                                }
                            }
                        }
                    }
                }
                if (args[0] == "!HELP")
                {
                    client.SendMessage(channel, $"/me : Help is on its way {e.ChatMessage.DisplayName}");
                }
                if (args[0] == "!SHOP")
                {
                    CheckNoXP(channel, e.ChatMessage.Username.ToUpper());
                    shop.Refresh();
                    XDocument Shop = XDocument.Load(@"Data\Shop.xml");
                    XElement S = Shop.Element("shop").Element("Global.Shop");
                    string Slot1 = S.Attribute("Slot1").Value;
                    string Slot2 = S.Attribute("Slot2").Value;
                    string Slot3 = S.Attribute("Slot3").Value;
                    string Cost1 = S.Attribute("Cost1").Value;
                    string Cost2 = S.Attribute("Cost2").Value;
                    string Cost3 = S.Attribute("Cost3").Value;
                    client.SendMessage(channel, "/me : Today's shop items are: 1. " + Slot1 + "(" + Cost1 + " Gold Coins) 2. " + Slot2 + "(" + Cost2 + " Gold Coins) 3. " + Slot3 + "(" + Cost3 + " Gold Coins)");
                }
                if (args[0] == "!GET" || args[0] == "!PURCHASE")
                {
                    if (args.Length < 2)
                    {
                        client.SendMessage(channel, "/me : You can't buy nothing, Select an item to buy!");
                    }
                    else
                    {
                        if (shop.IsBagFull(channel, e.ChatMessage.Username.ToUpper()))
                        {
                            client.SendMessage(channel, "/me : " + e.ChatMessage.Username + ", your bag seems to be full. Clear out some space and try again");
                        }
                        else if (Convert.ToInt32(args[1]) > 3)
                        {
                            client.SendMessage(channel, "/me : I think you made a mistake. There is only 3 items for sale here, you are trying to buy item " + args[1] + ".");
                        }
                        else
                        {
                            shop.BuyShopItem(channel, e.ChatMessage.Username.ToUpper(), Convert.ToInt32(args[1]));
                        }
                    }
                }
                if (args[0] == "!RIFT")
                {
                    rift.CheckChannel(channel);
                    string Status = null;
                    try
                    {
                        Status = XDocument.Load(@"Data\Rift.xml").Element("rift").Element("Channel." + channel).Attribute("Status").Value;
                    }
                    catch
                    {

                    }
                    if (args.Length == 1)
                        if (Status == "Off" || Status == null)
                        {
                            client.SendMessage(channel, "/me : There is no Rift active right now");
                        }
                        else
                        {
                            XDocument Rift = XDocument.Load(@"Data\Rift.xml");
                            XElement Ch = Rift.Element("rift").Element("Channel." + channel).Element(channel + ".Monster");
                            string Mon = Ch.Attribute("Monster").Value;
                            string Lvl = Ch.Attribute("MLvl").Value;
                            string Str = Ch.Attribute("MStr").Value;
                            string End = Ch.Attribute("MEnd").Value;
                            string Int = Ch.Attribute("MInt").Value;
                            string Dex = Ch.Attribute("MDex").Value;
                            string HP = Ch.Attribute("HP").Value;
                            string MHP = Ch.Attribute("MHP").Value;
                            client.SendMessage(channel, "/me : A rift is open. Current Lvl : (" + Lvl + "). " + Mon + " has (" + HP + "/" + MHP + ") HP Remaining. Loot Chest : (Lvl " + (Math.Floor(Convert.ToInt32(Lvl) / 5.0)) + ")");
                        }
                    else if (args.Length == 2)
                    {
                        if (args[1] == "OPEN" && e.ChatMessage.Username.ToUpper() == "T3MPU5_FU91T_")
                        {
                            rift.CreateRift(channel);
                        }
                        if (args[1] == "CLOSE" && e.ChatMessage.Username.ToUpper() == "T3MPU5_FU91T_")
                        {
                            rift.EndRift(channel);
                        }
                        if (args[1] == "JOIN")
                        {
                            if (Status == "Off" || Status == null)
                            {
                                client.SendMessage(channel, "/me : There is no Rift active right now");
                            }
                            else if (rift.IsPlaying(channel, e.ChatMessage.Username.ToUpper()))
                            {
                                client.SendMessage(channel, "/me : [RIFT] " + e.ChatMessage.DisplayName + ", You are already playing in this rift...");
                            }
                            else if (rift.CheckTokens(channel, e.ChatMessage.Username.ToUpper()) <= 0)
                            {
                                client.SendMessage(channel, "/me : [RIFT] You have no Rift tokens to enter this Rift!");
                            }
                            else
                            {
                                client.SendMessage(channel, "/me : [RIFT] " + e.ChatMessage.DisplayName + " has entered the rift. Good Luck!");
                                lock (_lockxp)
                                {
                                    XDocument XP = XDocument.Load(@"Data\XP.xml");
                                    XElement ele = XP.Element("users").Element(GetTopic(channel, e.ChatMessage.Username.ToUpper()));
                                    int Tokens = 0;
                                    try
                                    {
                                        Tokens = Convert.ToInt32(ele.Attribute("Tokens").Value);
                                    }
                                    catch
                                    {

                                    }
                                    ele.SetAttributeValue("Tokens", Tokens - 1);
                                    XP.Save(@"Data\XP.xml");
                                }
                                rift.AddPlayer(channel, e.ChatMessage.Username.ToUpper());
                            }
                        }
                    }
                }
                if (args[0] == "!TOKENS")
                {
                    int Tokens = rift.CheckTokens(channel, e.ChatMessage.Username.ToUpper());
                    client.SendMessage(channel, "/me : " + e.ChatMessage.DisplayName + " has " + Tokens + " Rift tokens.");
                }
                if (args[0] == "!ADDTOKEN" && e.ChatMessage.Username.ToUpper() == "T3MPU5_FU91T_")
                {
                    if (args.Length == 2)
                    {
                        string user = args[1].TrimStart('@');
                        CheckNoXP(channel, user);
                        lock (_lockxp)
                        {
                            XDocument XP = XDocument.Load(@"Data\XP.xml");
                            XElement ele = XP.Element("users").Element(GetTopic(channel, user));
                            int Tokens = 0;
                            try
                            {
                                Tokens = Convert.ToInt32(ele.Attribute("Tokens").Value);
                            }
                            catch
                            {

                            }
                            ele.SetAttributeValue("Tokens", Tokens + 1);
                            XP.Save(@"Data\XP.xml");
                        }
                        client.SendMessage(channel, "/me : Added 1 token to " + args[1].TrimStart('@').ToLower());
                    }
                }
                if (args[0] == "!ADVSTART")
                {
                    CheckNoXP(channel, e.ChatMessage.Username.ToUpper());
                    lock (_lockxp)
                    {
                        XDocument XP = XDocument.Load(@"Data\XP.xml");
                        XElement P = XP.Element("users").Element(GetTopic(channel, e.ChatMessage.Username.ToUpper()));
                        int Energy = Convert.ToInt32(P.Attribute("Energy").Value);
                        if (Energy <= 0)
                        {
                            client.SendMessage(channel, "/me : " + e.ChatMessage.DisplayName + " you have no energy to start an adventure!");
                        }
                        else
                        {
                            XDocument Adv = XDocument.Load(@"Data\Adventure.xml");
                            XElement A = Adv.Element("adventure").Element("Channel." + channel);
                            if (A.Attribute("Status").Value == "Running" || A.Attribute("Status").Value == "On")
                            {
                                client.SendMessage(channel, "/me : An adventure is already in progress.");
                            }
                            else
                            {
                                P.SetAttributeValue("Energy", (Energy - 1));
                                XP.Save(@"Data\XP.xml");
                                adventure.AddPlayer(channel, e.ChatMessage.Username.ToUpper());
                                Thread thread = new Thread(() => AdvStart(channel, e.ChatMessage.Username.ToUpper()));
                                thread.Start();
                            }
                        }
                    }
                }
                if (args[0] == "!RAIDSTART")
                {
                    CheckNoXP(channel, e.ChatMessage.Username.ToUpper());
                    lock (_lockxp)
                    {
                        XDocument XP = XDocument.Load(@"Data\XP.xml");
                        XElement P = XP.Element("users").Element(GetTopic(channel, e.ChatMessage.Username.ToUpper()));
                        int Energy = Convert.ToInt32(P.Attribute("Energy").Value);
                        if (Energy < 3)
                        {
                            client.SendMessage(channel, "/me : " + e.ChatMessage.DisplayName + " you haven't enough energy to start a raid!");
                        }
                        else if (args.Length < 2)
                        {
                            client.SendMessage(channel, "/me : Please specify a lvl of raid you want to start with !raidstart <lvl>.");
                        }
                        else if (!string.IsNullOrEmpty(args[1]) && !args[1].All(Char.IsDigit))
                        {
                            client.SendMessage(channel, "/me : This is not valid, stop trying to break me! Please specify a lvl of raid you want to start with !raidstart <lvl>.");
                        }
                        else if (Convert.ToInt32(args[1]) < 10000)
                        {
                            XDocument Adv = XDocument.Load(@"Data\Adventure.xml");
                            XElement A = Adv.Element("adventure").Element("Channel." + channel);
                            if (A.Attribute("Status").Value == "Running" || A.Attribute("Status").Value == "On")
                            {
                                client.SendMessage(channel, "/me : An adventure is already in progress.");
                            }
                            else
                            {
                                P.SetAttributeValue("Energy", Energy - 3);
                                XP.Save(@"Data\XP.xml");
                                adventure.SetRaid(channel);
                                adventure.AddPlayer(channel, e.ChatMessage.Username.ToUpper());
                                Thread thread = new Thread(() => RaidStart(channel, e.ChatMessage.Username.ToUpper(), Convert.ToInt32(args[1])));
                                thread.Start();
                            }
                        }
                        else
                        {
                            client.SendMessage(channel, "/me : Holy cow! You are really brave or really stupid, I can't go that high. Try again with a more sensible number!");
                        }
                    }
                }
                if (args[0] == "!ADVHARD")
                {
                    CheckNoXP(channel, e.ChatMessage.Username.ToUpper());
                    lock (_lockxp)
                    {
                        XDocument XP = XDocument.Load(@"Data\XP.xml");
                        XElement P = XP.Element("users").Element(GetTopic(channel, e.ChatMessage.Username.ToUpper()));
                        int Energy = Convert.ToInt32(P.Attribute("Energy").Value);
                        if (Energy <= 1)
                        {
                            client.SendMessage(channel, "/me : " + e.ChatMessage.DisplayName + " you haven't enough energy to start an adventure!");
                        }
                        else
                        {
                            XDocument Adv = XDocument.Load(@"Data\Adventure.xml");
                            XElement A = Adv.Element("adventure").Element("Channel." + channel);
                            if (A.Attribute("Status").Value == "Running" || A.Attribute("Status").Value == "On")
                            {
                                client.SendMessage(channel, "/me : An adventure is already in progress.");
                            }
                            else if (LookUpLvl(channel, e.ChatMessage.Username.ToUpper()) <= 49)
                            {
                                client.SendMessage(channel, "/me : " + e.ChatMessage.DisplayName + ", you must walk before you can run! Get some levels and try again.");
                            }
                            else
                            {
                                P.SetAttributeValue("Energy", (Energy - 2));
                                XP.Save(@"Data\XP.xml");
                                adventure.SetHard(channel);
                                adventure.AddPlayer(channel, e.ChatMessage.Username.ToUpper());
                                Thread thread = new Thread(() => AdvHard(channel, e.ChatMessage.Username.ToUpper()));
                                thread.Start();
                            }
                        }
                    }
                }
                /*if (args[0] == "!ADV150")
                {
                    CheckNoXP(channel, e.ChatMessage.Username.ToUpper());
                    lock (_lockxp)
                    {
                        XDocument XP = XDocument.Load(@"Data\XP.xml");
                        XElement P = XP.Element("users").Element(GetTopic(channel, e.ChatMessage.Username.ToUpper()));
                        int Energy = Convert.ToInt32(P.Attribute("Energy").Value);
                        if (Energy <= 1)
                        {
                            client.SendMessage(channel, "/me : " + e.ChatMessage.DisplayName + " you haven't enough energy to start an adventure!");
                        }
                        else
                        {
                            XDocument Adv = XDocument.Load(@"Data\Adventure.xml");
                            XElement A = Adv.Element("adventure").Element("Channel." + channel);
                            if (A.Attribute("Status").Value == "Running" || A.Attribute("Status").Value == "On")
                            {
                                client.SendMessage(channel, "/me : An adventure is already in progress.");
                            }
                            else if (LookUpLvl(channel, e.ChatMessage.Username.ToUpper()) <= 149)
                            {
                                client.SendMessage(channel, "/me : " + e.ChatMessage.DisplayName + ", you must walk before you can run! Get some levels and try again.");
                            }
                            else
                            {
                                P.SetAttributeValue("Energy", (Energy - 2));
                                XP.Save(@"Data\XP.xml");
                                adventure.Set150(channel);
                                adventure.AddPlayer(channel, e.ChatMessage.Username.ToUpper());
                                Thread thread = new Thread(() => Adv150(channel, e.ChatMessage.Username.ToUpper()));
                                thread.Start();
                            }
                        }
                    }
                }*/
                if (args[0] == "!ADVJOIN")
                {
                    CheckNoXP(channel, e.ChatMessage.Username.ToUpper());
                    lock (_lockxp)
                    {
                        XDocument XP = XDocument.Load(@"Data\XP.xml");
                        XElement P = XP.Element("users").Element(GetTopic(channel, e.ChatMessage.Username.ToUpper()));
                        int Energy = Convert.ToInt32(P.Attribute("Energy").Value);
                        XDocument Adv = XDocument.Load(@"Data\Adventure.xml");
                        XElement A = Adv.Element("adventure").Element("Channel." + channel);
                        if (A.Attribute("Status").Value == "Off")
                        {
                            client.SendMessage(channel, "/me : There isn't currently an adventure active! To start one type !advstart...");
                        }
                        else if (A.Attribute("Status").Value == "Running")
                        {
                            client.SendMessage(channel, "/me : This adventure has already started, please wait for the next one...");
                        }
                        else if (adventure.IsRaid(channel))
                        {
                            client.SendMessage(channel, "/me : Looks like this adventure is a raid... Join it with !raidjoin");
                        }
                        else if (adventure.PlayerList[channel].Contains(e.ChatMessage.Username.ToUpper()))
                        {
                            client.SendMessage(channel, "/me : What are you doing?? You are already in this adventure!");
                        }
                        else if (adventure.PlayerList[channel].Count == 4)
                        {
                            client.SendMessage(channel, "/me : Sorry, all places on this adventure have been taken! Please wait for the next one...");
                        }
                        else if (adventure.IsHard(channel))
                        {
                            if (Energy <= 1)
                            {
                                client.SendMessage(channel, "/me : " + e.ChatMessage.DisplayName + ", you haven't enough energy to join this adventure!");
                            }
                            else if (LookUpLvl(channel, e.ChatMessage.Username.ToUpper()) <= 49)
                            {
                                client.SendMessage(channel, "/me : " + e.ChatMessage.DisplayName + " you cannot join " + adventure.PlayerList[channel][0] + " on this adventure, their backpack isn't big enough to carry you!");
                            }
                            else if (adventure.PlayerList[channel].Count < 4)
                            {
                                adventure.AddPlayer(channel, e.ChatMessage.Username.ToUpper());
                                P.SetAttributeValue("Energy", Energy - 2);
                                client.SendMessage(channel, "/me : " + e.ChatMessage.DisplayName + " has joined the adventure! (" + (4 - adventure.PlayerList[channel].Count) + ") places left...");
                                XP.Save((@"Data\XP.xml"));
                            }
                        }
                        else if (adventure.Is150(channel))
                        {
                            if (Energy <= 1)
                            {
                                client.SendMessage(channel, "/me : " + e.ChatMessage.DisplayName + ", you haven't enough energy to join this adventure!");
                            }
                            else if (LookUpLvl(channel, e.ChatMessage.Username.ToUpper()) <= 149)
                            {
                                client.SendMessage(channel, "/me : " + e.ChatMessage.DisplayName + " you cannot join " + adventure.PlayerList[channel][0] + " on this adventure, their backpack isn't big enough to carry you!");
                            }
                            else if (adventure.PlayerList[channel].Count < 4)
                            {
                                adventure.AddPlayer(channel, e.ChatMessage.Username.ToUpper());
                                P.SetAttributeValue("Energy", Energy - 2);
                                client.SendMessage(channel, "/me : " + e.ChatMessage.DisplayName + " has joined the adventure! (" + (4 - adventure.PlayerList[channel].Count) + ") places left...");
                                XP.Save((@"Data\XP.xml"));
                            }
                        }
                        else
                        {
                            if (Energy <= 0)
                            {
                                client.SendMessage(channel, "/me : " + e.ChatMessage.DisplayName + ", you have no energy to join this adventure!");
                            }
                            else if (adventure.PlayerList[channel].Count < 4)
                            {
                                adventure.AddPlayer(channel, e.ChatMessage.Username.ToUpper());
                                P.SetAttributeValue("Energy", Energy - 1);
                                client.SendMessage(channel, "/me : " + e.ChatMessage.DisplayName + " has joined the adventure! (" + (4 - adventure.PlayerList[channel].Count) + ") places left...");
                                XP.Save((@"Data\XP.xml"));
                            }
                        }
                    }
                }
                if (args[0] == "!RAIDJOIN")
                {
                    CheckNoXP(channel, e.ChatMessage.Username.ToUpper());
                    lock (_lockxp)
                    {
                        XDocument XP = XDocument.Load(@"Data\XP.xml");
                        XElement P = XP.Element("users").Element(GetTopic(channel, e.ChatMessage.Username.ToUpper()));
                        int Energy = Convert.ToInt32(P.Attribute("Energy").Value);
                        XDocument Adv = XDocument.Load(@"Data\Adventure.xml");
                        XElement A = Adv.Element("adventure").Element("Channel." + channel);
                        if (A.Attribute("Status").Value == "Off")
                        {
                            client.SendMessage(channel, "/me : There isn't currently a raid active! To start one type !raidstart...");
                        }
                        else if (A.Attribute("Status").Value == "Running")
                        {
                            client.SendMessage(channel, "/me : This adventure has already started, please wait for the next one...");
                        }
                        else if (!adventure.IsRaid(channel))
                        {
                            client.SendMessage(channel, "/me : Looks like this adventure is an adventure... Join it with !advjoin");
                        }
                        else if (adventure.PlayerList[channel].Contains(e.ChatMessage.Username.ToUpper()))
                        {
                            client.SendMessage(channel, "/me : What are you doing?? You are already in this raid!");
                        }
                        else if (adventure.PlayerList[channel].Count == 8)
                        {
                            client.SendMessage(channel, "/me : Sorry, all places on this raid have been taken! Please wait for the next one...");
                        }
                        else if (Energy <= 2)
                        {
                            client.SendMessage(channel, "/me : " + e.ChatMessage.DisplayName + ", you haven't enough energy to join this raid!");
                        }
                        else if (adventure.PlayerList[channel].Count < 8)
                        {
                            adventure.AddPlayer(channel, e.ChatMessage.Username.ToUpper());
                            P.SetAttributeValue("Energy", Energy - 3);
                            client.SendMessage(channel, "/me : " + e.ChatMessage.DisplayName + " has joined the adventure! (" + (8 - adventure.PlayerList[channel].Count) + ") places left...");
                            XP.Save((@"Data\XP.xml"));
                        }
                    }
                }
                if (args[0] == "!SKPOINTS")
                {
                    if (args.Length == 2 && args[1] == "RESET")
                    {
                        int Coins = Convert.ToInt32(shop.GetSlotCoins(channel, e.ChatMessage.Username.ToUpper()).Substring(0, shop.GetSlotCoins(channel, e.ChatMessage.Username.ToUpper()).IndexOf('.')));
                        string Slot = shop.GetSlotCoins(channel, e.ChatMessage.Username.ToUpper()).Substring(shop.GetSlotCoins(channel, e.ChatMessage.Username.ToUpper()).IndexOf('.') + 1);
                        if (Coins >= 200)
                        {
                            lock (_lockxp)
                            {
                                XDocument XP = XDocument.Load(@"Data\XP.xml");
                                XElement P = XP.Element("users").Element(GetTopic(channel, e.ChatMessage.Username.ToUpper()));
                                lock (_lockstats)
                                {
                                    XDocument Stats = XDocument.Load(@"Data\Stats.xml");
                                    XElement P2 = Stats.Element("stats").Element(GetTopic(channel, e.ChatMessage.Username.ToUpper()));
                                    P2.SetAttributeValue("HP", "100");
                                    P2.SetAttributeValue("Strength", "5");
                                    P2.SetAttributeValue("Archery", "5");
                                    P2.SetAttributeValue("Endurance", "5");
                                    P2.SetAttributeValue("Intelligence", "5");
                                    P2.SetAttributeValue("Dexterity", "5");
                                    P2.SetAttributeValue("Looting", "5");
                                    Stats.Save(@"Data\Stats.xml");
                                }
                                if (LookUpLvl(channel, e.ChatMessage.Username.ToUpper()) > 100)
                                {
                                    P.SetAttributeValue("SkPoints", ((Convert.ToInt32(P.Attribute("Lvl").Value) - 99) * 10) + 495);
                                }
                                else
                                {
                                    P.SetAttributeValue("SkPoints", (Convert.ToInt32(P.Attribute("Lvl").Value) - 1) * 5);
                                }
                                XP.Save(@"Data\XP.xml");
                            }
                            lock (_lockloot)
                            {
                                XDocument Loot = XDocument.Load(@"Data\Loot.xml");
                                XElement P3 = Loot.Element("loot").Element(GetTopic(channel, e.ChatMessage.Username.ToUpper()));
                                if (Coins > 200)
                                {
                                    P3.SetAttributeValue("Slot" + Slot, Coins - 200 + " Gold Coins");
                                }
                                else
                                {
                                    P3.SetAttributeValue("Slot" + Slot, null);
                                }
                                Loot.Save(@"Data\Loot.xml");
                            }
                            client.SendMessage(channel, "/me : You have reset your skill points!");
                        }
                        else
                        {
                            client.SendMessage(channel, "/me : You cannot afford to reset your stats, this cost 200 Gold Coins.");
                        }
                    }
                    else if (args.Length == 1)
                    {
                        CheckNoXP(channel, e.ChatMessage.Username.ToUpper());
                        XDocument XP;
                        lock (_lockxp)
                        {
                            XP = XDocument.Load(@"Data\XP.xml");
                        }
                        XElement P = XP.Element("users").Element(GetTopic(channel, e.ChatMessage.Username.ToUpper()));
                        int Points = Convert.ToInt32(P.Attribute("SkPoints").Value);
                        client.SendMessage(channel, "/me : " + e.ChatMessage.DisplayName + " has (" + Points + ") Skill Points.");
                    }
                }
                if (args[0] == "!UPGRADE")
                {
                    CheckNoXP(channel, e.ChatMessage.Username.ToUpper());
                    adventure.CheckLootUser(channel, e.ChatMessage.Username.ToUpper());
                    lock (_lockxp)
                    {
                        XDocument XP = XDocument.Load(@"Data\XP.xml");
                        XElement P = XP.Element("users").Element(GetTopic(channel, e.ChatMessage.Username.ToUpper()));
                        lock (_lockstats)
                        {
                            XDocument Stats = XDocument.Load(@"Data\Stats.xml");
                            XElement P2 = Stats.Element("stats").Element(GetTopic(channel, e.ChatMessage.Username.ToUpper()));
                            int Points = Convert.ToInt32(P.Attribute("SkPoints").Value);
                            lock (_lockloot)
                            {
                                XDocument Data = XDocument.Load(@"Data\LootData.xml");
                                XElement getData = Data.Element("loot").Element("upgrades").Element("shards");
                                XDocument Loot = XDocument.Load(@"Data\Loot.xml");
                                XElement P3 = Loot.Element("loot").Element(GetTopic(channel, e.ChatMessage.Username.ToUpper())).Element("Materials");
                                List<string> Items = new List<string> { "WEAPON", "HELM", "CHEST", "LEGS", "BOOTS" };
                                Dictionary<string, string> Next = new Dictionary<string, string> { { "WOODEN", "STEEL" }, { "STEEL", "MOONSTONE" }, { "MOONSTONE", "FIRE" }, { "FIRE", "ENCHANTED" } };
                                if (args.Length < 2)
                                {
                                    client.SendMessage(channel, "/me : Please select a skill/item to upgrade.");
                                }
                                else if (!Items.Contains(args[1]) && Points == 0)
                                {
                                    client.SendMessage(channel, "/me : You have 0 Skill Points available");
                                }
                                else if (args.Length > 2 && !string.IsNullOrEmpty(args[2]) && args[2].All(Char.IsDigit) && Convert.ToInt32(args[2]) > Points)
                                {
                                    client.SendMessage(channel, "/me : You don't have enough Skill Points available for that");
                                }
                                else if (args[1] == "STR" || args[1] == "STRENGTH")
                                {
                                    if (args.Length > 2 && regex.IsMatch(args[2]))
                                    {
                                        P.SetAttributeValue("SkPoints", Points - Convert.ToInt32(args[2]));
                                        P2.SetAttributeValue("Strength", adventure.GetPlayerStr(channel, e.ChatMessage.Username.ToUpper()) + Convert.ToInt32(args[2]));
                                        client.SendMessage(channel, "/me : Strength has been upgraded by " + args[2] + ", Strength is now at " + (adventure.GetPlayerStr(channel, e.ChatMessage.Username.ToUpper()) + Convert.ToInt32(args[2])));
                                    }
                                    else if (args.Length == 2)
                                    {
                                        P.SetAttributeValue("SkPoints", Points - 1);
                                        P2.SetAttributeValue("Strength", adventure.GetPlayerStr(channel, e.ChatMessage.Username.ToUpper()) + 1);
                                        client.SendMessage(channel, "/me : Strength has been upgraded by 1, Strength is now at " + (adventure.GetPlayerStr(channel, e.ChatMessage.Username.ToUpper()) + 1));
                                    }
                                }
                                else if (args[1] == "END" || args[1] == "ENDURANCE")
                                {
                                    if (args.Length > 2 && regex.IsMatch(args[2]))
                                    {
                                        P.SetAttributeValue("SkPoints", Points - Convert.ToInt32(args[2]));
                                        P2.SetAttributeValue("Endurance", adventure.GetPlayerEnd(channel, e.ChatMessage.Username.ToUpper()) + Convert.ToInt32(args[2]));
                                        client.SendMessage(channel, "/me : Endurance has been upgraded by " + args[2] + ", Endurance is now at " + (adventure.GetPlayerEnd(channel, e.ChatMessage.Username.ToUpper()) + Convert.ToInt32(args[2])));
                                    }
                                    else if (args.Length == 2)
                                    {
                                        P.SetAttributeValue("SkPoints", Points - 1);
                                        P2.SetAttributeValue("Endurance", adventure.GetPlayerEnd(channel, e.ChatMessage.Username.ToUpper()) + 1);
                                        client.SendMessage(channel, "/me : Endurance has been upgraded by 1, Endurance is now at " + (adventure.GetPlayerEnd(channel, e.ChatMessage.Username.ToUpper()) + 1));
                                    }
                                }
                                else if (args[1] == "INT" || args[1] == "INTELLIGENCE")
                                {
                                    if (args.Length > 2 && regex.IsMatch(args[2]))
                                    {
                                        P.SetAttributeValue("SkPoints", Points - Convert.ToInt32(args[2]));
                                        P2.SetAttributeValue("Intelligence", adventure.GetPlayerInt(channel, e.ChatMessage.Username.ToUpper()) + Convert.ToInt32(args[2]));
                                        client.SendMessage(channel, "/me : Intelligence has been upgraded by " + args[2] + ", Intelligence is now at " + (adventure.GetPlayerInt(channel, e.ChatMessage.Username.ToUpper()) + Convert.ToInt32(args[2])));
                                    }
                                    else if (args.Length == 2)
                                    {
                                        P.SetAttributeValue("SkPoints", Points - 1);
                                        P2.SetAttributeValue("Intelligence", adventure.GetPlayerInt(channel, e.ChatMessage.Username.ToUpper()) + 1);
                                        client.SendMessage(channel, "/me : Intelligence has been upgraded by 1, Intelligence is now at " + (adventure.GetPlayerInt(channel, e.ChatMessage.Username.ToUpper()) + 1));
                                    }
                                }
                                else if (args[1] == "DEX" || args[1] == "DEXTERITY")
                                {
                                    if (args.Length > 2 && regex.IsMatch(args[2]))
                                    {
                                        P.SetAttributeValue("SkPoints", Points - Convert.ToInt32(args[2]));
                                        P2.SetAttributeValue("Dexterity", adventure.GetPlayerDex(channel, e.ChatMessage.Username.ToUpper()) + Convert.ToInt32(args[2]));
                                        client.SendMessage(channel, "/me : Dexterity has been upgraded by " + args[2] + ", Dexterity is now at " + (adventure.GetPlayerDex(channel, e.ChatMessage.Username.ToUpper()) + Convert.ToInt32(args[2])));
                                    }
                                    else if (args.Length == 2)
                                    {
                                        P.SetAttributeValue("SkPoints", Points - 1);
                                        P2.SetAttributeValue("Dexterity", adventure.GetPlayerDex(channel, e.ChatMessage.Username.ToUpper()) + 1);
                                        client.SendMessage(channel, "/me : Dexterity has been upgraded by 1, Dexterity is now at " + (adventure.GetPlayerDex(channel, e.ChatMessage.Username.ToUpper()) + 1));
                                    }
                                }
                                else if (args[1] == "FAI" || args[1] == "FAITH")
                                {
                                    if (args.Length > 2 && regex.IsMatch(args[2]))
                                    {
                                        P.SetAttributeValue("SkPoints", Points - Convert.ToInt32(args[2]));
                                        P2.SetAttributeValue("Faith", adventure.GetPlayerFai(channel, e.ChatMessage.Username.ToUpper()) + Convert.ToInt32(args[2]));
                                        client.SendMessage(channel, "/me : Faith has been upgraded by " + args[2] + ", Faith is now at " + (adventure.GetPlayerFai(channel, e.ChatMessage.Username.ToUpper()) + Convert.ToInt32(args[2])));
                                    }
                                    else if (args.Length == 2)
                                    {
                                        P.SetAttributeValue("SkPoints", Points - 1);
                                        P2.SetAttributeValue("Faith", adventure.GetPlayerFai(channel, e.ChatMessage.Username.ToUpper()) + 1);
                                        client.SendMessage(channel, "/me : Faith has been upgraded by 1, Faith is now at " + (adventure.GetPlayerFai(channel, e.ChatMessage.Username.ToUpper()) + 1));
                                    }
                                }
                                else if (args[1] == "SPE" || args[1] == "SPEED")
                                {
                                    if (args.Length > 2 && regex.IsMatch(args[2]))
                                    {
                                        P.SetAttributeValue("SkPoints", Points - Convert.ToInt32(args[2]));
                                        P2.SetAttributeValue("Speed", adventure.GetPlayerSpe(channel, e.ChatMessage.Username.ToUpper()) + Convert.ToInt32(args[2]));
                                        client.SendMessage(channel, "/me : Speed has been upgraded by " + args[2] + ", Speed is now at " + (adventure.GetPlayerSpe(channel, e.ChatMessage.Username.ToUpper()) + Convert.ToInt32(args[2])));
                                    }
                                    else if (args.Length == 2)
                                    {
                                        P.SetAttributeValue("SkPoints", Points - 1);
                                        P2.SetAttributeValue("Speed", adventure.GetPlayerSpe(channel, e.ChatMessage.Username.ToUpper()) + 1);
                                        client.SendMessage(channel, "/me : Speed has been upgraded by 1, Speed is now at " + (adventure.GetPlayerSpe(channel, e.ChatMessage.Username.ToUpper()) + 1));
                                    }
                                }
                                else if (args[1] == "HP" || args[1] == "HEALTH")
                                {
                                    if (args.Length > 2 && regex.IsMatch(args[2]))
                                    {
                                        P.SetAttributeValue("SkPoints", Points - Convert.ToInt32(args[2]));
                                        P2.SetAttributeValue("HP", adventure.GetPlayerHP(channel, e.ChatMessage.Username.ToUpper()) + (Convert.ToInt32(args[2]) * 5));
                                        client.SendMessage(channel, "/me : HP has been upgraded by " + (Convert.ToInt32(args[2]) * 5) + ", HP is now at " + (adventure.GetPlayerHP(channel, e.ChatMessage.Username.ToUpper()) + (Convert.ToInt32(args[2]) * 5)));
                                    }
                                    else if (args.Length == 2)
                                    {
                                        P.SetAttributeValue("SkPoints", Points - 1);
                                        P2.SetAttributeValue("HP", adventure.GetPlayerHP(channel, e.ChatMessage.Username.ToUpper()) + 5);
                                        client.SendMessage(channel, "/me : HP has been upgraded by 5, HP is now at " + (adventure.GetPlayerHP(channel, e.ChatMessage.Username.ToUpper()) + 5));
                                    }
                                }
                                else if (args[1] == "ARC" || args[1] == "ARCHERY")
                                {
                                    if (args.Length > 2 && regex.IsMatch(args[2]))
                                    {
                                        P.SetAttributeValue("SkPoints", Points - Convert.ToInt32(args[2]));
                                        P2.SetAttributeValue("Archery", adventure.GetPlayerArc(channel, e.ChatMessage.Username.ToUpper()) + Convert.ToInt32(args[2]));
                                        client.SendMessage(channel, "/me : Archery has been upgraded by " + Convert.ToInt32(args[2]) + ", Archery is now at " + (adventure.GetPlayerArc(channel, e.ChatMessage.Username.ToUpper()) + Convert.ToInt32(args[2])));
                                    }
                                    else if (args.Length == 2)
                                    {
                                        P.SetAttributeValue("SkPoints", Points - 1);
                                        P2.SetAttributeValue("Archery", adventure.GetPlayerArc(channel, e.ChatMessage.Username.ToUpper()) + 1);
                                        client.SendMessage(channel, "/me : Archery has been upgraded by 1, Archery is now at " + (adventure.GetPlayerArc(channel, e.ChatMessage.Username.ToUpper()) + 1));
                                    }
                                }
                                else if (args[1] == "LOO" || args[1] == "LOOTING")
                                {
                                    if (args.Length > 2 && regex.IsMatch(args[2]))
                                    {
                                        P.SetAttributeValue("SkPoints", Points - Convert.ToInt32(args[2]));
                                        P2.SetAttributeValue("Looting", adventure.GetPlayerLoo(channel, e.ChatMessage.Username.ToUpper()) + Convert.ToInt32(args[2]));
                                        client.SendMessage(channel, "/me : Looting has been upgraded by " + Convert.ToInt32(args[2]) + ", Looting is now at " + (adventure.GetPlayerLoo(channel, e.ChatMessage.Username.ToUpper()) + Convert.ToInt32(args[2])));
                                    }
                                    else if (args.Length == 2)
                                    {
                                        P.SetAttributeValue("SkPoints", Points - 1);
                                        P2.SetAttributeValue("Looting", adventure.GetPlayerLoo(channel, e.ChatMessage.Username.ToUpper()) + 1);
                                        client.SendMessage(channel, "/me : Looting has been upgraded by 1, Looting is now at " + (adventure.GetPlayerLoo(channel, e.ChatMessage.Username.ToUpper()) + 1));
                                    }
                                }
                                else if (args[1] == "WEAPON")
                                {
                                    string Sword = adventure.GetWeapon(channel, e.ChatMessage.Username.ToUpper());
                                    int position = Sword.IndexOf(" ");
                                    string type = Sword.Substring(0, position).ToUpper();
                                    int Shards = 0;
                                    try
                                    {
                                        Shards = Convert.ToInt32(P3.Attribute(char.ToUpper(Next[type].ToLower()[0]) + Next[type].ToLower().Substring(1)).Value);
                                    }
                                    catch
                                    {

                                    }
                                    if (getData.Attributes(type).FirstOrDefault() == null)
                                    {
                                        client.SendMessage(channel, "/me : You cannot upgrade this Weapon!");
                                    }
                                    else
                                    {
                                        double Cost = Convert.ToDouble(getData.Attribute(type).Value);
                                        if (Cost > Shards)
                                        {
                                            client.SendMessage(channel, "/me : You don't have enough shards to upgrade this " + Sword);
                                        }
                                        else
                                        {
                                            XElement sword = Loot.Element("loot").Element(GetTopic(channel, e.ChatMessage.Username.ToUpper()));
                                            sword.SetAttributeValue("Sword", Sword.Replace(char.ToUpper(type.ToLower()[0]) + type.ToLower().Substring(1), char.ToUpper(Next[type].ToLower()[0]) + Next[type].ToLower().Substring(1)));
                                            P3.SetAttributeValue(char.ToUpper(Next[type].ToLower()[0]) + Next[type].ToLower().Substring(1), Shards - Cost);
                                            client.SendMessage(channel, "/me : You have upgraded your " + Sword + " to a " + Sword.Replace(char.ToUpper(type.ToLower()[0]) + type.ToLower().Substring(1), char.ToUpper(Next[type].ToLower()[0]) + Next[type].ToLower().Substring(1)) + "!");
                                        }
                                    }
                                }
                                else if (args[1] == "HELM")
                                {
                                    string Helm = adventure.GetHelm(channel, e.ChatMessage.Username.ToUpper());
                                    int position = Helm.IndexOf(" ");
                                    string type = Helm.Substring(0, position).ToUpper();
                                    int Shards = 0;
                                    try
                                    {
                                        Shards = Convert.ToInt32(P3.Attribute(char.ToUpper(Next[type].ToLower()[0]) + Next[type].ToLower().Substring(1)).Value);
                                    }
                                    catch
                                    {

                                    }
                                    if (getData.Attributes(type).FirstOrDefault() == null)
                                    {
                                        client.SendMessage(channel, "/me : You cannot upgrade this Helm!");
                                    }
                                    else
                                    {
                                        double Cost = Convert.ToDouble(getData.Attribute(type).Value);
                                        if (Cost > Shards)
                                        {
                                            client.SendMessage(channel, "/me : You don't have enough shards to upgrade this " + Helm);
                                        }
                                        else
                                        {
                                            XElement helm = Loot.Element("loot").Element(GetTopic(channel, e.ChatMessage.Username.ToUpper()));
                                            helm.SetAttributeValue("Helm", Helm.Replace(char.ToUpper(type.ToLower()[0]) + type.ToLower().Substring(1), char.ToUpper(Next[type].ToLower()[0]) + Next[type].ToLower().Substring(1)));
                                            P3.SetAttributeValue(char.ToUpper(Next[type].ToLower()[0]) + Next[type].ToLower().Substring(1), Shards - Cost);
                                            client.SendMessage(channel, "/me : You have upgraded your " + Helm + " to a " + Helm.Replace(char.ToUpper(type.ToLower()[0]) + type.ToLower().Substring(1), char.ToUpper(Next[type].ToLower()[0]) + Next[type].ToLower().Substring(1)) + "!");
                                        }
                                    }
                                }
                                else if (args[1] == "CHEST")
                                {
                                    string Chest = adventure.GetChest(channel, e.ChatMessage.Username.ToUpper());
                                    int position = Chest.IndexOf(" ");
                                    string type = Chest.Substring(0, position).ToUpper();
                                    int Shards = 0;
                                    try
                                    {
                                        Shards = Convert.ToInt32(P3.Attribute(char.ToUpper(Next[type].ToLower()[0]) + Next[type].ToLower().Substring(1)).Value);
                                    }
                                    catch
                                    {

                                    }
                                    if (getData.Attributes(type).FirstOrDefault() == null)
                                    {
                                        client.SendMessage(channel, "/me : You cannot upgrade this Chest!");
                                    }
                                    else
                                    {
                                        double Cost = Convert.ToDouble(getData.Attribute(type).Value);
                                        if (Cost > Shards)
                                        {
                                            client.SendMessage(channel, "/me : You don't have enough shards to upgrade this " + Chest);
                                        }
                                        else
                                        {
                                            XElement chest = Loot.Element("loot").Element(GetTopic(channel, e.ChatMessage.Username.ToUpper()));
                                            chest.SetAttributeValue("Chest", Chest.Replace(char.ToUpper(type.ToLower()[0]) + type.ToLower().Substring(1), char.ToUpper(Next[type].ToLower()[0]) + Next[type].ToLower().Substring(1)));
                                            P3.SetAttributeValue(char.ToUpper(Next[type].ToLower()[0]) + Next[type].ToLower().Substring(1), Shards - Cost);
                                            client.SendMessage(channel, "/me : You have upgraded your " + Chest + " to a " + Chest.Replace(char.ToUpper(type.ToLower()[0]) + type.ToLower().Substring(1), char.ToUpper(Next[type].ToLower()[0]) + Next[type].ToLower().Substring(1)) + "!");
                                        }
                                    }
                                }
                                else if (args[1] == "LEGS")
                                {
                                    string Legs = adventure.GetLegs(channel, e.ChatMessage.Username.ToUpper());
                                    int position = Legs.IndexOf(" ");
                                    string type = Legs.Substring(0, position).ToUpper();
                                    int Shards = 0;
                                    try
                                    {
                                        Shards = Convert.ToInt32(P3.Attribute(char.ToUpper(Next[type].ToLower()[0]) + Next[type].ToLower().Substring(1)).Value);
                                    }
                                    catch
                                    {

                                    }
                                    if (getData.Attributes(type).FirstOrDefault() == null)
                                    {
                                        client.SendMessage(channel, "/me : You cannot upgrade these Legs!");
                                    }
                                    else
                                    {
                                        double Cost = Convert.ToDouble(getData.Attribute(type).Value);
                                        if (Cost > Shards)
                                        {
                                            client.SendMessage(channel, "/me : You don't have enough shards to upgrade these " + Legs);
                                        }
                                        else
                                        {
                                            XElement legs = Loot.Element("loot").Element(GetTopic(channel, e.ChatMessage.Username.ToUpper()));
                                            legs.SetAttributeValue("Legs", Legs.Replace(char.ToUpper(type.ToLower()[0]) + type.ToLower().Substring(1), char.ToUpper(Next[type].ToLower()[0]) + Next[type].ToLower().Substring(1)));
                                            P3.SetAttributeValue(char.ToUpper(Next[type].ToLower()[0]) + Next[type].ToLower().Substring(1), Shards - Cost);
                                            client.SendMessage(channel, "/me : You have upgraded your " + Legs + " to a " + Legs.Replace(char.ToUpper(type.ToLower()[0]) + type.ToLower().Substring(1), char.ToUpper(Next[type].ToLower()[0]) + Next[type].ToLower().Substring(1)) + "!");
                                        }
                                    }
                                }
                                else if (args[1] == "BOOTS")
                                {
                                    string Boots = adventure.GetBoots(channel, e.ChatMessage.Username.ToUpper());
                                    int position = Boots.IndexOf(" ");
                                    string type = Boots.Substring(0, position).ToUpper();
                                    int Shards = 0;
                                    try
                                    {
                                        Shards = Convert.ToInt32(P3.Attribute(char.ToUpper(Next[type].ToLower()[0]) + Next[type].ToLower().Substring(1)).Value);
                                    }
                                    catch
                                    {

                                    }
                                    if (getData.Attributes(type).FirstOrDefault() == null)
                                    {
                                        client.SendMessage(channel, "/me : You cannot upgrade these Boots!");
                                    }
                                    else
                                    {
                                        double Cost = Convert.ToDouble(getData.Attribute(type).Value);
                                        if (Cost > Shards)
                                        {
                                            client.SendMessage(channel, "/me : You don't have enough shards to upgrade these " + Boots);
                                        }
                                        else
                                        {
                                            XElement boots = Loot.Element("loot").Element(GetTopic(channel, e.ChatMessage.Username.ToUpper()));
                                            boots.SetAttributeValue("Boots", Boots.Replace(char.ToUpper(type.ToLower()[0]) + type.ToLower().Substring(1), char.ToUpper(Next[type].ToLower()[0]) + Next[type].ToLower().Substring(1)));
                                            P3.SetAttributeValue(char.ToUpper(Next[type].ToLower()[0]) + Next[type].ToLower().Substring(1), Shards - Cost);
                                            client.SendMessage(channel, "/me : You have upgraded your " + Boots + " to a " + Boots.Replace(char.ToUpper(type.ToLower()[0]) + type.ToLower().Substring(1), char.ToUpper(Next[type].ToLower()[0]) + Next[type].ToLower().Substring(1)) + "!");
                                        }
                                    }
                                }
                                Loot.Save(@"Data\Loot.xml");
                            }
                            XP.Save(@"Data\XP.xml");
                            Stats.Save(@"Data\Stats.xml");
                        }
                    }
                }
                if (args[0] == "!ENERGY")
                {
                    CheckNoXP(channel, e.ChatMessage.Username.ToUpper());
                    XDocument XP;
                    lock (_lockxp)
                    {
                        XP = XDocument.Load(@"Data\XP.xml");
                    }
                    XElement P = XP.Element("users").Element(GetTopic(channel, e.ChatMessage.Username.ToUpper()));
                    int Energy = Convert.ToInt32(P.Attribute("Energy").Value);
                    client.SendMessage(channel, "/me : " + e.ChatMessage.Username + " has (" + Energy + ") energy.");
                }
                if (args[0] == "!ADDENERGY")
                {
                    if (channel == e.ChatMessage.Username.ToUpper() || e.ChatMessage.Username.ToUpper() == "MEELUXBOT" || e.ChatMessage.Username.ToUpper() == "T3MPU5_FU91T_")
                    {
                        CheckNoXP(channel, e.ChatMessage.Username.ToUpper());
                        if (args.Length == 1)
                        {
                            client.SendMessage(channel, "/me : Please specify a user.");
                        }
                        else if (args.Length == 2 && args[1] == "ALL")
                        {
                            client.SendMessage(channel, "/me : Added 1 energy to all users online!");
                            foreach (string user in OnlineUsers)
                            {
                                int position = user.IndexOf(".");
                                string Channel = user.Substring(0, position);
                                string User = user.Substring(position + 1);
                                if (Channel == channel)
                                {
                                    AddEnergy(channel, User);
                                }
                            }
                        }
                        else if (args.Length == 2)
                        {
                            client.SendMessage(channel, "/me : Please specify an amount to give.");
                        }
                        else
                        {
                            client.SendMessage(channel, "/me : Added " + args[2] + " energy to " + args[1].ToLower().TrimStart('@') + ".");
                            lock (_lockxp)
                            {
                                XDocument XP = XDocument.Load(@"Data\XP.xml");
                                XElement P = XP.Element("users").Element(GetTopic(channel, args[1].TrimStart('@')));
                                int Energy = Convert.ToInt32(P.Attribute("Energy").Value);
                                P.SetAttributeValue("Energy", Energy + Convert.ToInt32(args[2]));
                                XP.Save(@"Data\XP.xml");
                            }
                        }
                    }
                    else
                    {
                        client.SendMessage(channel, "/me : You are not permitted to use this command!");
                    }
                }
                if (args[0] == "!XP")
                {
                    if (args.Length > 1)
                    {

                        CheckNoXP(channel, args[1].TrimStart('@'));
                        client.SendMessage(channel, "/me : " + args[1].ToLower().TrimStart('@') + " has (" + LookUpXP(channel, args[1].TrimStart('@')) + ") XP and is LVL (" + LookUpLvl(channel, args[1].TrimStart('@')) + "). [" + LookUpNextLvl(channel, args[1].TrimStart('@')) + " XP to next LVL]");
                    }
                    else
                    {
                        CheckNoXP(channel, e.ChatMessage.Username.ToUpper());
                        client.SendMessage(channel, "/me : " + e.ChatMessage.DisplayName + " has (" + LookUpXP(channel, e.ChatMessage.Username.ToUpper()) + ") XP and is LVL (" + LookUpLvl(channel, e.ChatMessage.Username.ToUpper()) + "). [" + LookUpNextLvl(channel, e.ChatMessage.Username.ToUpper()) + " XP to next LVL]");
                    }
                }
                if (args[0] == "!STATS" || args[0] == "!SKILLS")
                {
                    CheckNoXP(channel, e.ChatMessage.Username.ToUpper());
                    string HP = adventure.GetPlayerHP(channel, e.ChatMessage.Username.ToUpper()).ToString();
                    string STR = adventure.GetPlayerStr(channel, e.ChatMessage.Username.ToUpper()).ToString();
                    string ARC = adventure.GetPlayerArc(channel, e.ChatMessage.Username.ToUpper()).ToString();
                    string SPE = adventure.GetPlayerSpe(channel, e.ChatMessage.Username.ToUpper()).ToString();
                    string END = adventure.GetPlayerEnd(channel, e.ChatMessage.Username.ToUpper()).ToString();
                    string INT = adventure.GetPlayerInt(channel, e.ChatMessage.Username.ToUpper()).ToString();
                    string DEX = adventure.GetPlayerDex(channel, e.ChatMessage.Username.ToUpper()).ToString();
                    string FAI = adventure.GetPlayerFai(channel, e.ChatMessage.Username.ToUpper()).ToString();
                    string LOO = adventure.GetPlayerLoo(channel, e.ChatMessage.Username.ToUpper()).ToString();
                    client.SendMessage(channel, "/me : " + e.ChatMessage.DisplayName + " (Lvl " + LookUpLvl(channel, e.ChatMessage.Username.ToUpper()) + ") has HP: (" + HP + "), Strength: (" + STR + "), Archery: (" + ARC + "), Speed: (" + SPE + "), Endurance: (" + END + "), Intelligence: (" + INT + "), Dexterity: (" + DEX + "), Faith: (" + FAI + "), Looting: (" + LOO + ")");
                }
                if (args[0] == "!LVLON")
                {
                    CheckNoXP(channel, e.ChatMessage.Username.ToUpper());
                    lock (_lockxp)
                    {
                        XDocument XP = XDocument.Load(@"Data\XP.xml");
                        XElement P = XP.Element("users").Element(GetTopic(channel, e.ChatMessage.Username.ToUpper()));
                        P.SetAttributeValue("OptIn", "Yes");
                        XP.Save(@"Data\XP.xml");
                    }
                    client.SendMessage(channel, "/me : You have opted in to Lvl Up broadcasts.");
                }
                if (args[0] == "!LVLOFF")
                {
                    CheckNoXP(channel, e.ChatMessage.Username.ToUpper());
                    lock (_lockxp)
                    {
                        XDocument XP = XDocument.Load(@"Data\XP.xml");
                        XElement P = XP.Element("users").Element(GetTopic(channel, e.ChatMessage.Username.ToUpper()));
                        P.SetAttributeValue("OptIn", "No");
                        XP.Save(@"Data\XP.xml");
                    }
                    client.SendMessage(channel, "/me : You have opted out of Lvl Up broadcasts.");
                }
                if (args[0] == "!INFO")
                {
                    CheckNoXP(channel, e.ChatMessage.Username.ToUpper());
                    if (args.Length == 1)
                    {
                        client.SendMessage(channel, "/me : Use !info <***> to find information about certain commands/items.");
                    }
                    else if (args[1] == "MYSTERIOUS")
                    {
                        client.SendMessage(channel, "/me : There is no information on the Mysterious Sword of Unknown Power. There are rumours that it cannot be tamed!");
                    }
                    else if (args[1] == "WOODEN")
                    {
                        client.SendMessage(channel, "/me : Wooden items have +5 % Def for armour and +10 % Atk for weapons.");
                    }
                    else if (args[1] == "STEEL")
                    {
                        client.SendMessage(channel, "/me : Steel items have +10 % Def for armour and +20 % Atk for weapons.");
                    }
                    else if (args[1] == "MOONSTONE")
                    {
                        client.SendMessage(channel, "/me : Moonstone items have +15 % Def for armour and +35 % Atk for weapons.");
                    }
                    else if (args[1] == "FIRE")
                    {
                        client.SendMessage(channel, "/me : Fire items have +20 % Def for armour and +50 % Atk for weapons.");
                    }
                    else if (args[1] == "ENCHANTED")
                    {
                        client.SendMessage(channel, "/me : Enchanted items have +22 % Def for armour and +60 % Atk for weapons.");
                    }
                    else if (args[1] == "PAPER")
                    {
                        client.SendMessage(channel, "/me : Paper items have +0 % Def for armour and -10 % Atk for weapons.");
                    }
                    else if (args[1] == "RED")
                    {
                        client.SendMessage(channel, "/me : Red items have +1 % Def for armour and +5 % Atk for weapons.");
                    }
                    else if (args[1] == "BLUE")
                    {
                        client.SendMessage(channel, "/me : Blue items have +1 % Def for armour and +5 % Atk for weapons.");
                    }
                    else if (args[1] == "GLASS")
                    {
                        client.SendMessage(channel, "/me : Glass items have +2 % Def for armour and +2 % Atk for weapons.");
                    }
                    else if (args[1] == "ICE")
                    {
                        client.SendMessage(channel, "/me : Ice items have +2 % Def for armour and +2 % Atk for weapons.");
                    }
                    else if (args[1] == "LEATHER")
                    {
                        client.SendMessage(channel, "/me : Leather items have +6 % Def for armour and +0 % Atk for weapons.");
                    }
                    else if (args[1] == "BRASS")
                    {
                        client.SendMessage(channel, "/me : Brass items have +8 % Def for armour and +17 % Atk for weapons.");
                    }
                    else if (args[1] == "DARK")
                    {
                        client.SendMessage(channel, "/me : Dark items have +12 % Def for armour and +25 % Atk for weapons.");
                    }
                    else if (args[1] == "GOLDEN")
                    {
                        client.SendMessage(channel, "/me : Golden items have +14 % Def for armour and +32 % Atk for weapons.");
                    }
                    else if (args[1] == "EBONY")
                    {
                        client.SendMessage(channel, "/me : Ebony items have +15 % Def for armour and +35 % Atk for weapons.");
                    }
                    else if (args[1] == "DRAGON")
                    {
                        client.SendMessage(channel, "/me : Dragon Bone items have + 17 % Def for armour and +40 % Atk for weapons.");
                    }
                    else if (args[1] == "SPIRIT")
                    {
                        client.SendMessage(channel, "/me : Spirit items have +18 % Def for armour and +42 % Atk for weapons.");
                    }
                    else if (args[1] == "DEMONIC")
                    {
                        client.SendMessage(channel, "/me : Demonic items have +24 % Def for armour and +70 % Atk for weapons.");
                    }
                    else if (args[1] == "VIBRANIUM")
                    {
                        client.SendMessage(channel, "/me : Vibranium items have +25 % Def for armour and +80 % Atk for weapons.");
                    }
                    else if (args[1] == "INFINTY")
                    {
                        client.SendMessage(channel, "/me : Infinity items have +30 % Def for armour and +100 % Atk for weapons.");
                    }
                    else if (args[1] == "HARD")
                    {
                        client.SendMessage(channel, "/me : Hard adventures are for players above Lvl 50, they take 2 energy and have much tougher bosses.The chance for high tier loot is far greater. Good luck!");
                    }
                    else if (args[1] == "PRECISION")
                    {
                        client.SendMessage(channel, "/me : The Ring of Precision will give a +20% chance of crit.");
                    }
                    else if (args[1] == "AVOIDANCE")
                    {
                        client.SendMessage(channel, "/me : The Ring of Avoidance will give a +20% chance of dodge.");
                    }
                    else if (args[1] == "PROTECTION")
                    {
                        client.SendMessage(channel, "/me : The Ring of Protection will give +20% defence.");
                    }
                    else if (args[1] == "TRAINING")
                    {
                        client.SendMessage(channel, "/me : The Ring of Training will give +50% XP.");
                    }
                    else if (args[1] == "MIGHT")
                    {
                        client.SendMessage(channel, "/me : The Ring of Might will give +20% attack.");
                    }
                    else if (args[1] == "RECOVERY")
                    {
                        client.SendMessage(channel, "/me : The Ring of Recovery will give a small chance of healing 20% HP.");
                    }
                    else if (args[1] == "SPOILS")
                    {
                        client.SendMessage(channel, "/me : The Ring of Spoils will give an increased chance of higher tier loot.");
                    }
                    else if (args[1] == "SETS" || args[1] == "SET")
                    {
                        client.SendMessage(channel, "/me : Completing a 'set', irrespective of material, will give a stats boost while equipped.");
                    }
                    else if (args[1] == "ERIDIN")
                    {
                        client.SendMessage(channel, "/me : The Eredin set will increase stats by 5%");
                    }
                    else if (args[1] == "BRAHMA")
                    {
                        client.SendMessage(channel, "/me : The Brahma set will increase stats by 7.5%");
                    }
                    else if (args[1] == "NIGHT")
                    {
                        client.SendMessage(channel, "/me : The Night and Day set will increase stats by 10%");
                    }
                    else if (args[1] == "DESTINY")
                    {
                        client.SendMessage(channel, "/me : The Destiny 2.0 set will increase stats by 15%");
                    }
                    else if (args[1] == "ENLIGHTENMENT")
                    {
                        client.SendMessage(channel, "/me : The Enlightenment set will increase stats by 8%");
                    }
                    else if (args[1] == "HIDDEN" || args[1] == "MYSTERIES")
                    {
                        client.SendMessage(channel, "/me : The Hidden Mysteries set will increase stats by 12%");
                    }
                    else if (args[1] == "LOST" || args[1] == "SOULS")
                    {
                        client.SendMessage(channel, "/me : The Lost Souls set will increase stats by 14%");
                    }
                    else if (args[1] == "AXE")
                    {
                        client.SendMessage(channel, "/me : Axes give an increase in attack, but a slight sacrifice in defense!");
                    }
                    else if (args[1] == "SWORD")
                    {
                        client.SendMessage(channel, "/me : Swords are a nice all rounder, with no attack/defense modifiers.");
                    }
                    else if (args[1] == "HAMMER")
                    {
                        client.SendMessage(channel, "/me : Hammers give an increase in defense, but a slight sacrifice in attack!");
                    }
                    else if (args[1] == "BOW")
                    {
                        client.SendMessage(channel, "/me : Bows will require archery skill, they have an increased chance to dodge, but will have a chance to miss at low level.");
                    }
                    else if (args[1] == "ARCHERY")
                    {
                        client.SendMessage(channel, "/me : Archery skill is used for wielding a bow. Increase this skill to lower your chance of missing!");
                    }
                    else if (args[1] == "UPGRADE")
                    {
                        client.SendMessage(channel, "/me : Upgrade you equipment with shards. You can only go up 1 tier at a time. It costs 25 Steel Shards to upgrade to Steel, 35 Moonstone Shards to upgrade to Moonstone, 45 Fire Shards to upgrade to Fire, 50 Enchanted Shards to upgrade to Enchanted.");
                    }
                    else if (args[1] == "SMELT")
                    {
                        client.SendMessage(channel, "/me : Smelting items that belong to a 'set' will give you shards of that particular material... Smelting 25 shards will give you 1 shard of the next tier.");
                    }
                    else
                    {
                        client.SendMessage(channel, "/me : There is no info on this right now.");
                        File.AppendAllText(@"Data\Info.txt", args[1] + "\n");
                    }
                }
                if (args[0] == "!KILL")
                {
                    CheckNoXP(channel, e.ChatMessage.Username.ToUpper());
                    if (args.Length < 2)
                    {
                        client.SendMessage(channel, "/me : you need to choose somebody to kill!");
                    }
                    else
                    {
                        if (shop.CheckOutlaw(channel, args[1].ToUpper().TrimStart('@')))
                        {
                            if (OnlineUsers.Contains(channel + "." + args[1].ToUpper().TrimStart('@')))
                            {
                                client.SendMessage(channel, "/me : " + e.ChatMessage.DisplayName + " is attempting to capture and kill " + args[1].ToLower().TrimStart('@'));
                                Thread thread = new Thread(() => shop.TryKill(channel, e.ChatMessage.Username.ToUpper(), args[1].ToUpper().TrimStart('@')));
                                thread.Start();
                            }
                            else
                            {
                                client.SendMessage(channel, "/me : " + args[1].ToLower().TrimStart('@') + " isn't anywhere to be seen. Look again later!");
                            }
                        }
                        else
                        {
                            client.SendMessage(channel, "/me : " + args[1].ToLower().TrimStart('@') + " isn't an outlaw, killing innocent people is forbidden!");
                        }
                    }
                }
                if (args[0] == "!STEAL")
                {
                    CheckNoXP(channel, e.ChatMessage.Username.ToUpper());
                    shop.Refresh();
                    if (shop.CanSteal(channel, e.ChatMessage.Username.ToUpper()))
                    {
                        if (!shop.IsBagFull(channel, e.ChatMessage.Username.ToUpper()))
                        {
                            client.SendMessage(channel, "/me : Attempting to steal...");
                            Thread thread = new Thread(() => shop.TrySteal(channel, e.ChatMessage.Username.ToUpper()));
                            thread.Start();
                        }
                        else
                        {
                            client.SendMessage(channel, "/me : Your bag is full, clear some space and try again");
                        }
                    }
                    else
                    {
                        client.SendMessage(channel, "/me : You cannot steal anything right now!");
                    }
                }
                if (args[0] == "!OUTLAWS")
                {
                    CheckNoXP(channel, e.ChatMessage.Username.ToUpper());
                    shop.ListOutlaws(channel, e.ChatMessage.Username.ToUpper());
                }
                if (args[0] == "!JOIN" || args[0] == "!INSTALL")
                {
                    if (channel == "T3MPBOT")
                    {
                        string Channel;
                        if ((args.Length == 2 && e.ChatMessage.Username.ToUpper() == args[1]) || (args.Length == 2 && e.ChatMessage.Username.ToUpper() == "T3MPU5_FU91T_"))
                        {
                            Channel = $"{args[1].ToLower()}";
                        }
                        else
                        {
                            Channel = e.ChatMessage.Username;
                        }
                        lock (_lockchann)
                        {
                            XDocument Ch = XDocument.Load(@"Data\Channels.xml");
                            XElement J = Ch.Element("channels").Element("joined");
                            J.SetAttributeValue(Channel, "Yes");
                            Ch.Save(@"Data\Channels.xml");
                        }
                        client.SendMessage(channel, "/me : Installed bot on channel " + Channel + ". Thank you for using T3mpbot.");
                        client.JoinChannel(Channel);
                    }
                }
                if (args[0] == "!LEAVE" || args[0] == "!UNINSTALL")
                {
                    if (channel == "T3MPBOT")
                    {
                        string Channel;
                        if ((args.Length == 2 && e.ChatMessage.Username.ToUpper() == args[1]) || (args.Length == 2 && e.ChatMessage.Username.ToUpper() == "T3MPU5_FU91T_"))
                        {
                            Channel = $"{args[1].ToLower()}";
                        }
                        else
                        {
                            Channel = e.ChatMessage.Username;
                        }
                        lock (_lockchann)
                        {
                            XDocument Ch = XDocument.Load(@"Data\Channels.xml");
                            XElement J = Ch.Element("channels").Element("joined");
                            J.SetAttributeValue(Channel, null);
                            Ch.Save(@"Data\Channels.xml");
                        }
                        client.SendMessage(channel, "/me : Uninstalled bot on channel " + Channel + ". We're sorry to leave you.");
                        client.LeaveChannel(Channel);
                    }
                }
                if (args[0] == "!BOP")
                {
                    if (rift.boss.Contains(channel))
                    {
                        rift.GiveShards(channel, e.ChatMessage.Username.ToUpper());
                    }
                }
                if (args[0] == "!RESETBOP")
                {
                    if (e.ChatMessage.Username.ToUpper().Equals("T3MPU5_FU91T_"))
                    {
                        Thread NewBoss = new Thread(() => rift.Boss2(channel, args[1].TrimStart('@')));
                        NewBoss.Start();
                    }
                }
        }

        private void Client_OnLog(object sender, OnLogArgs e)
        {
            Console.WriteLine(e.Data);
        }

        private void Client_OnConnectionError(object sender, OnConnectionErrorArgs e)
        {
            Console.WriteLine($"Error!! {e.Error}");
        }

        internal void Disconnect()
        {
            Console.WriteLine("Disconnecting...");
        }

    }
}
