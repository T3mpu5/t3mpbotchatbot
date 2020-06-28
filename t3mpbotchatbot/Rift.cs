using TwitchLib.Api.V5.Models.Streams;
using TwitchLib.Api;
using TwitchLib.Api.Services;
using TwitchLib.Api.V5.Models.Channels;
using System.Security.Cryptography.X509Certificates;
using TwitchLib.PubSub;
using System;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Threading;
using System.Globalization;
using TwitchLib.PubSub.Events;

namespace t3mpbotchatbot
{
    public class Rift
    {
        TwitchAPI api = new TwitchAPI();
        LiveStreamMonitorService monitor;
        public TwitchPubSub pubsub = new TwitchPubSub();
        Random random = new Random(Guid.NewGuid().GetHashCode());
        Adventure adventure = new Adventure();
        public List<string> boss = new List<string>();

        public async void Connect()
        {
            api.Settings.ClientId = TwitchInfo.ClientID;
            api.Settings.AccessToken = TwitchInfo.BotToken;
            monitor = new LiveStreamMonitorService(api, 60);
            XDocument CH = XDocument.Load(@"Data\Channels.xml");
            XElement ele = CH.Element("channels").Element("joined");
            List<string> list = new List<string>();
            foreach (XAttribute atr in ele.Attributes())
            {
                var foundChannelResponse = await api.V5.Users.GetUserByNameAsync(atr.Name.ToString().ToLower());
                var foundChannel = foundChannelResponse.Matches.FirstOrDefault();
                Console.WriteLine("FoundID : " + foundChannel.Id + ", Name : " + foundChannel.Name);
                list.Add(foundChannel.Id);
            }
            list.Add("222544855");
            monitor.SetChannelsById(list);
            monitor.OnStreamOnline += Monitor_OnStreamOnline;
            monitor.OnStreamOffline += Monitor_OnStreamOffline;
            monitor.Start();
            pubsub.OnPubSubServiceConnected += Pubsub_OnPubSubServiceConnected;
            pubsub.OnPubSubServiceError += Pubsub_OnPubSubServiceError;
            pubsub.OnPubSubServiceClosed += Pubsub_OnPubSubServiceClosed;
            pubsub.OnListenResponse += Pubsub_OnListenResponse;
            pubsub.OnBitsReceived += Pubsub_OnBitsReceived;
            pubsub.Connect();
            pubsub.SendTopics(TwitchInfo.BotToken);
        }

        private void Pubsub_OnPubSubServiceClosed(object sender, EventArgs e)
        {
            Console.WriteLine("Service Closed...");
        }

        private void Pubsub_OnPubSubServiceError(object sender, OnPubSubServiceErrorArgs e)
        {
            Console.WriteLine(e.Exception);
        }

        private async void Pubsub_OnListenResponse(object sender, OnListenResponseArgs e)
        {
            if (e.Successful)
            {
                Console.WriteLine($"Successfully verified listening to topic: {e.Topic}");
                System.Threading.Thread.Sleep(300);
                var channelName = await api.V5.Channels.GetChannelByIDAsync(e.Topic.Substring(e.Topic.IndexOf('.')).TrimStart('.'));
                Console.WriteLine("channel name: " + channelName.Name.ToString());
            }
            else
            {
                var channelName = await api.V5.Channels.GetChannelByIDAsync(e.Topic.Substring(e.Topic.IndexOf('.')).TrimStart('.'));
                Console.WriteLine($"Failed to listen! Error: {e.Response.Error}");
                Main.client.SendWhisper(channelName.Name.ToString(), "There was an error, please contact @t3mpu5_fu91t_ for support.");
            }
        }

        private void Pubsub_OnBitsReceived(object sender, TwitchLib.PubSub.Events.OnBitsReceivedArgs e)
        {
            Console.WriteLine("Bits Rec : " + e.BitsUsed + " , Total bits : " + e.TotalBitsUsed);
            if (e.BitsUsed >= 1000)
            {
                if (!boss.Contains(e.ChannelName.ToUpper()))
                {
                    Thread NewBoss = new Thread(() => Boss(e));
                    NewBoss.Start();
                }
            }
        }

        private void Boss(TwitchLib.PubSub.Events.OnBitsReceivedArgs e)
        {
            boss.Add(e.ChannelName.ToUpper());
            Main.client.SendMessage(e.ChannelName, "/me : For " + e.Username + "'s geneosity, " + e.ChannelName + " can now be 'Bopped'. Use !bop to Bop BOP " + e.ChannelName + " and receive shards!");
            Thread.Sleep(30000);
            boss.Remove(e.ChannelName.ToUpper());
            Main.client.SendMessage(e.ChannelName, "/me : " + e.ChannelName + " has gone away to hide! Thanks again " + e.Username + " for the bits!");
        }
        public void Boss2(string channel, string user)
        {
            boss.Add(channel.ToUpper());
            Main.client.SendMessage(channel, "/me : Reset bops for " + user.ToLower() + ", " + channel.ToLower() + " can now be 'Bopped'. Use !bop to Bop BOP " + channel.ToLower() + " and receive shards!");
            Thread.Sleep(30000);
            boss.Remove(channel.ToUpper());
            Main.client.SendMessage(channel, "/me : " + channel.ToLower() + " has gone away to hide! Thanks again " + user.ToLower() + " for the bits!");
        }

        private void Pubsub_OnPubSubServiceConnected(object sender, EventArgs e)
        {
            Console.WriteLine("Connected PubSub...");
            pubsub.ListenToBitsEvents("222544855");
            pubsub.SendTopics(TwitchInfo.BotToken);
            Thread.Sleep(2000);
            Listen();
        }

        public async void Listen()
        {
            XDocument CH = XDocument.Load(@"Data\Channels.xml");
            XElement ele = CH.Element("channels").Element("joined");
            foreach (XAttribute atr in ele.Attributes())
            {
                var foundChannelResponse = await api.V5.Users.GetUserByNameAsync(atr.Name.ToString().ToLower());
                var foundChannel = foundChannelResponse.Matches.FirstOrDefault();
                if (atr.Value != "Yes")
                {
                    Console.WriteLine("trying Listen to " + atr.Name);
                    pubsub.ListenToBitsEvents(foundChannel.Id);
                    pubsub.SendTopics(atr.Value);
                    Thread.Sleep(2000);
                }
            }
        }

        private async void Monitor_OnStreamOffline(object sender, TwitchLib.Api.Services.Events.LiveStreamMonitor.OnStreamOfflineArgs e)
        {
            var foundChannelResponse = await api.V5.Channels.GetChannelByIDAsync(e.Channel);
            var foundChannel = foundChannelResponse.Name.ToUpper();
            if (IsRift(foundChannel))
            {
                EndRift(foundChannel);
                Thread.Sleep(3000);
                ClearRift(foundChannel);
            }
        }

        private async void Monitor_OnStreamOnline(object sender, TwitchLib.Api.Services.Events.LiveStreamMonitor.OnStreamOnlineArgs e)
        {
            var foundChannelResponse = await api.V5.Channels.GetChannelByIDAsync(e.Channel);
            var foundChannel = foundChannelResponse.Name.ToUpper();
            if (!IsRift(foundChannel))
            {
                CreateRift(foundChannel);
            }
        }

        public void CheckChannel(string channel)
        {
            lock (Main._lockrift)
            {
                XDocument Rift = XDocument.Load(@"Data\Rift.xml");
                var ele = Rift.Element("rift").Elements("Channel." + channel).SingleOrDefault();
                if (ele == null)
                {
                    Rift.Element("rift").Add(new XElement("Channel." + channel));
                    Rift.Save(@"Data\Rift.xml");
                }
            }
        }

        private void CheckMonster(string channel)
        {
            lock (Main._lockrift)
            {
                XDocument Rift = XDocument.Load(@"Data\Rift.xml");
                XElement P = Rift.Element("rift").Element("Channel." + channel);
                var ele2 = P.Elements(channel + ".Monster").SingleOrDefault();
                if (ele2 == null)
                {
                    P.Add(new XElement(channel + ".Monster"));
                    Rift.Save(@"Data\Rift.xml");
                }
            }
        }

        private void CheckPlayers(string channel)
        {
            lock (Main._lockrift)
            {
                XDocument Rift = XDocument.Load(@"Data\Rift.xml");
                XElement P = Rift.Element("rift").Element("Channel." + channel);
                var ele2 = P.Elements(channel + ".Players").SingleOrDefault();
                if (ele2 == null)
                {
                    P.Add(new XElement(channel + ".Players"));
                    Rift.Save(@"Data\Rift.xml");
                }
            }
        }

        private void CheckUser(string channel, string user)
        {
            lock (Main._lockrift)
            {
                XDocument Rift = XDocument.Load(@"Data\Rift.xml");
                XElement P = Rift.Element("rift").Element("Channel." + channel).Element(channel + ".Players");
                var ele2 = P.Elements(channel + "." + user).SingleOrDefault();
                if (ele2 == null)
                {
                    P.Add(new XElement(channel + "." + user));
                    Rift.Save(@"Data\Rift.xml");
                }
            }
        }

        public void CheckMatsUser(string channel, string user)
        {
            adventure.CheckLootUser(channel, user);
            lock (Main._lockloot)
            {
                XDocument Loot = XDocument.Load(@"Data\Loot.xml");
                XElement P = Loot.Element("loot").Element(Main.GetTopic(channel, user));
                var ele2 = P.Elements("Materials").SingleOrDefault();
                if (ele2 == null)
                {
                    P.Add(new XElement("Materials"));
                    Loot.Save(@"Data\Loot.xml");
                }
            }
        }

        public bool IsRift(string channel)
        {
            string Status = null;
            try
            {
                Status = XDocument.Load(@"Data\Rift.xml").Element("rift").Element("Channel." + channel).Attribute("Status").Value;
            }
            catch
            {

            }
            return Status == "On";
        }

        public int GetSteelShards(string channel, string user)
        {
            XElement loot = XElement.Load(@"Data\Loot.xml");
            int Slot1 = 0;
            foreach (XElement ele in loot.Descendants(Main.GetTopic(channel, user)))
            {
                try
                {
                    Slot1 = Convert.ToInt32(ele.Element("Materials").Attribute("Steel").Value);
                }
                catch
                {
                    Slot1 = 0;
                }
            }
            return Slot1;
        }

        public int GetMoonShards(string channel, string user)
        {
            XElement loot = XElement.Load(@"Data\Loot.xml");
            int Slot1 = 0;
            foreach (XElement ele in loot.Descendants(Main.GetTopic(channel, user)))
            {
                try
                {
                    Slot1 = Convert.ToInt32(ele.Element("Materials").Attribute("Moonstone").Value);
                }
                catch
                {
                    Slot1 = 0;
                }
            }
            return Slot1;
        }

        public int GetFireShards(string channel, string user)
        {
            XElement loot = XElement.Load(@"Data\Loot.xml");
            int Slot1 = 0;
            foreach (XElement ele in loot.Descendants(Main.GetTopic(channel, user)))
            {
                try
                {
                    Slot1 = Convert.ToInt32(ele.Element("Materials").Attribute("Fire").Value);
                }
                catch
                {
                    Slot1 = 0;
                }
            }
            return Slot1;
        }

        public int GetEnchShards(string channel, string user)
        {
            XElement loot = XElement.Load(@"Data\Loot.xml");
            int Slot1 = 0;
            foreach (XElement ele in loot.Descendants(Main.GetTopic(channel, user)))
            {
                try
                {
                    Slot1 = Convert.ToInt32(ele.Element("Materials").Attribute("Enchanted").Value);
                }
                catch
                {
                    Slot1 = 0;
                }
            }
            return Slot1;
        }

        private int GetRiftPlayerHP(string channel, string user)
        {
            lock (Main._lockrift)
            {
                XElement Rift = XElement.Load(@"Data\Rift.xml");
                int PHP = 0;
                foreach (XElement ele in Rift.Descendants(channel + "." + user))
                {
                    PHP = Convert.ToInt32(ele.Attribute("HP").Value);
                }
                if (PHP > 0)
                {
                    return PHP;
                }
                else
                {
                    return 0;
                }
            }
        }

        private int GetMonsterLvl(string channel)
        {
            lock (Main._lockrift)
            {
                XElement Rift = XElement.Load(@"Data\Rift.xml");
                int MLvl = 0;
                foreach (XElement ele in Rift.Descendants(channel + ".Monster"))
                {
                    MLvl = Convert.ToInt32(ele.Attribute("MLvl").Value);
                }
                if (MLvl > 0)
                {
                    return MLvl;
                }
                else
                {
                    return 0;
                }
            }
        }

        private int GetMonsterDex(string channel)
        {
            lock (Main._lockrift)
            {
                XElement Adv = XElement.Load(@"Data\Rift.xml");
                int MDex = 0;
                foreach (XElement ele in Adv.Descendants(channel + ".Monster"))
                {
                    MDex = Convert.ToInt32(ele.Attribute("MDex").Value);
                }
                if (MDex > 0)
                {
                    return MDex;
                }
                else
                {
                    return 0;
                }
            }
        }

        private int GetMonsterInt(string channel)
        {
            lock (Main._lockrift)
            {
                XElement Adv = XElement.Load(@"Data\Rift.xml");
                int MInt = 0;
                foreach (XElement ele in Adv.Descendants(channel + ".Monster"))
                {
                    MInt = Convert.ToInt32(ele.Attribute("MInt").Value);
                }
                if (MInt > 0)
                {
                    return MInt;
                }
                else
                {
                    return 0;
                }
            }
        }

        private int GetMonsterStr(string channel)
        {
            lock (Main._lockrift)
            {
                XElement Adv = XElement.Load(@"Data\Rift.xml");
                int MStr = 0;
                foreach (XElement ele in Adv.Descendants(channel + ".Monster"))
                {
                    MStr = Convert.ToInt32(ele.Attribute("MStr").Value);
                }
                if (MStr > 0)
                {
                    return MStr;
                }
                else
                {
                    return 0;
                }
            }
        }

        private int GetMonsterEnd(string channel)
        {
            lock (Main._lockrift)
            {
                XElement Adv = XElement.Load(@"Data\Rift.xml");
                int MEnd = 0;
                foreach (XElement ele in Adv.Descendants(channel + ".Monster"))
                {
                    MEnd = Convert.ToInt32(ele.Attribute("MEnd").Value);
                }
                if (MEnd > 0)
                {
                    return MEnd;
                }
                else
                {
                    return 0;
                }
            }
        }

        private int GetMonsterHP(string channel)
        {
            lock (Main._lockrift)
            {
                XElement Rift = XElement.Load(@"Data\Rift.xml");
                int MHP = 0;
                foreach (XElement ele in Rift.Descendants(channel + ".Monster"))
                {
                    MHP = Convert.ToInt32(ele.Attribute("HP").Value);
                }
                if (MHP > 0)
                {
                    return MHP;
                }
                else
                {
                    return 0;
                }
            }
        }

        private void AddToken(string channel)
        {
            lock (Main._lockxp)
            {
                XDocument XP = XDocument.Load(@"Data\XP.xml");
                XElement ele = XP.Element("users");
                int Tokens;
                foreach (XElement usr in ele.Elements())
                {
                    Tokens = 0;
                    if (Main.GetTopic(channel, "T3MPBOT").Contains("GLOBAL"))
                    {
                        if (usr.Name.ToString().Substring(0, usr.Name.ToString().IndexOf('.')) == "GLOBAL")
                        {
                            try
                            {
                                Tokens = Convert.ToInt32(usr.Attribute("Tokens").Value);
                            }
                            catch
                            {

                            }
                            usr.SetAttributeValue("Tokens", Tokens + 1);
                        }
                    }
                    else
                    {
                        if (usr.Name.ToString().Substring(0, usr.Name.ToString().IndexOf('.')) == channel)
                        {
                            try
                            {
                                Tokens = Convert.ToInt32(usr.Attribute("Tokens").Value);
                            }
                            catch
                            {

                            }
                            usr.SetAttributeValue("Tokens", Tokens + 1);
                        }
                    }
                }
                XP.Save(@"Data\XP.xml");
            }
        }

        public int CheckTokens(string channel, string user)
        {
            lock (Main._lockxp)
            {
                XDocument XP = XDocument.Load(@"Data\XP.xml");
                XElement ele = XP.Element("users").Element(Main.GetTopic(channel, user));
                int Tokens = 0;
                try
                {
                    Tokens = Convert.ToInt32(ele.Attribute("Tokens").Value);
                }
                catch
                {

                }
                return Tokens;
            }
        }

        private void GenRiftMonster(string channel, int level)
        {
            string Monster = MakeRiftMonster();
            string MLvl = level.ToString();
            string MHP = (level * 350).ToString();
            string MStr = Math.Ceiling((level + 5) / 1.28).ToString();
            string MEnd = Math.Ceiling((level + 5) / 0.95).ToString();
            string MInt = Math.Ceiling((level + 4) / 1.43).ToString();
            string MDex = Math.Ceiling((level + 4) / 1.39).ToString();
            CheckChannel(channel);
            CheckMonster(channel);
            lock (Main._lockrift)
            {
                XDocument Rift = XDocument.Load(@"Data\Rift.xml");
                XElement monster = Rift.Element("rift").Element("Channel." + channel).Element(channel + ".Monster");
                monster.SetAttributeValue("Monster", Monster);
                monster.SetAttributeValue("MLvl", MLvl);
                monster.SetAttributeValue("MHP", MHP);
                monster.SetAttributeValue("MStr", MStr);
                monster.SetAttributeValue("MEnd", MEnd);
                monster.SetAttributeValue("MInt", MInt);
                monster.SetAttributeValue("MDex", MDex);
                monster.SetAttributeValue("HP", MHP);
                Rift.Save(@"Data\Rift.xml");
            }
        }

        private string MakeRiftMonster()
        {
            string[] prefixes = { "Pizza", "Astro", "Red", "Higher", "Elemental", "Science", "Nerd", "Glowing", "Light", "Green", "Dirty", "Indecisive", "Ridiculous", "Enormous", "Orange" };
            int indexPre = random.Next(prefixes.Length);
            string[] types = { "Skull", "Reaper", "Shredder", "Pooper", "Jumper", "Grabber", "Licker", "Growler", "Piercer", "Pusher", "Crusher", "Fighter" };
            int indexTyp = random.Next(types.Length);
            string prefix = prefixes[indexPre];
            string type = " " + types[indexTyp];
            return prefix + type;
        }

        public async void CreateRift(string channel)
        {
            ClearRift(channel);
            GenRiftMonster(channel, 1);
            await Task.Run(() => AddToken(channel));
            lock (Main._lockrift)
            {
                XDocument Rift = XDocument.Load(@"Data\Rift.xml");
                XElement P = Rift.Element("rift").Element("Channel." + channel);
                P.SetAttributeValue("Status", "On");
                Rift.Save(@"Data\Rift.xml");
            }
            Main.client.SendMessage(channel, "/me : [RIFT] A rift has been created! Use tokens to enter the rift...");
        }

        public bool IsPlaying(string channel, string user)
        {
            CheckPlayers(channel);
            CheckUser(channel, user);
            bool Play;
            lock (Main._lockrift)
            {
                XDocument Rift = XDocument.Load(@"Data\Rift.xml");
                XElement ele = Rift.Element("rift").Element("Channel." + channel).Element(channel + ".Players").Element(channel + "." + user);
                Play = false;
                if (ele.Attributes("Playing").SingleOrDefault() != null)
                {
                    if (ele.Attribute("Playing").Value == "Yes")
                    {
                        Play = true;
                    }
                }
            }
            return Play;
        }

        public void AddPlayer(string channel, string user)
        {
            CheckPlayers(channel);
            CheckUser(channel, user);
            lock (Main._lockrift)
            {
                XDocument Rift = XDocument.Load(@"Data\Rift.xml");
                XElement ele = Rift.Element("rift").Element("Channel." + channel).Element(channel + ".Players").Element(channel + "." + user);
                ele.SetAttributeValue("HP", adventure.GetPlayerHP(channel, user));
                ele.SetAttributeValue("Dmg", 0);
                ele.SetAttributeValue("Playing", "Yes");
                Rift.Save(@"Data\Rift.xml");
            }
            StartAttack(channel, user);
        }

        private void StartAttack(string channel, string user)
        {
            int PHP = GetRiftPlayerHP(channel, user);
            while (PHP > 0)
            {
                DoAttack(channel, user);
                Thread.Sleep(3000);
                DoMAttack(channel, user);
                PHP = GetRiftPlayerHP(channel, user);
                Thread.Sleep(3000);
            }
            EndPlayer(channel, user);
        }

        public bool IsCrit(string channel, string user)
        {
            double critChance = (((adventure.GetPlayerInt(channel, user) + adventure.GetPlayerDex(channel, user) / 2) / GetMonsterDex(channel)) * 5);
            critChance = Convert.ToDouble(critChance.ToString("#.000"));
            if (adventure.GetRing(channel, user).Contains("Precision"))
            {
                critChance *= 1.2;
            }
            if (random.Next(1, 100000) <= (critChance * 1000))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public double GenDodgeChance(string channel, string user)
        {
            double dodgeChance = (((adventure.GetPlayerDex(channel, user) * 1.3) / (GetMonsterInt(channel) + GetMonsterDex(channel) * 0.8)) * 4);
            dodgeChance = Convert.ToDouble(dodgeChance.ToString("#.000"));
            if (adventure.GetRing(channel, user).Contains("Avoidance"))
            {
                dodgeChance *= 1.2;
            }
            return dodgeChance;
        }

        private void DoAttack(string channel, string player)
        {
            string weapon = adventure.GetWeapon(channel, player);
            int AttackDmg = adventure.GetAttackDmg(channel, player);
            if (IsCrit(channel, player))
            {
                AttackDmg *= 2;
                WriteMHP(channel, AttackDmg);

                Main.client.SendWhisper(player, "[CRITICAL] " + player.ToLower() + " has done (" + AttackDmg + ") dmg with their " + weapon + ". (" + GetMonsterHP(channel) + "HP Remaining)");
            }
            else
            {
                WriteMHP(channel, AttackDmg);

                Main.client.SendWhisper(player, player.ToLower() + " has done (" + AttackDmg + ") dmg with their " + weapon + ". (" + GetMonsterHP(channel) + "HP Remaining)");
            }
            WriteAttackDmg(channel, player, AttackDmg);
            if (adventure.GetRing(channel, player).Contains("Recovery"))
            {
                TryHeal(channel, player);
            }
        }

        public int GenMAttack(string channel, string user)
        {
            double[] token = { 1.9, 2, 2.1, 2.2, 2.3, 2.4 };
            int Atk = Convert.ToInt32(Math.Floor((((GetMonsterStr(channel) * 2) + (GetMonsterEnd(channel) * 2.5) * token[random.Next(token.Length)])) / adventure.GetDefenseMultiplier(channel, user)));
            return Atk;
        }

        private void DoMAttack(string channel, string target)
        {
            string monster = null;
            lock (Main._lockrift)
            {
                XElement Rift = XElement.Load(@"Data\Rift.xml");
                foreach (XElement ele in Rift.Descendants(channel + ".Monster"))
                {
                    monster = ele.Attribute("Monster").Value.ToString();
                }
            }
            int AttackDmg = GenMAttack(channel, target);
            if (GetMonsterHP(channel) > 0)
            {
                if ((GenDodgeChance(channel, target) * 1000) >= random.Next(1, 100000))
                {
                    Main.client.SendWhisper(target, "[DODGE] " + target.ToLower() + " has dodged the attack!");
                }
                else
                {
                    WritePlayerHP(channel, target, AttackDmg);
                    Main.client.SendWhisper(target, monster + " has done (" + AttackDmg + ") dmg to " + target.ToLower() + ". (" + GetRiftPlayerHP(channel, target) + "HP Remaining)");
                    System.Threading.Thread.Sleep(100);
                    if (GetRiftPlayerHP(channel, target) <= 0)
                    {
                        System.Threading.Thread.Sleep(100);
                        Main.client.SendMessage(channel, "/me : [RIFT] " + target.ToLower() + " is now dead!");
                    }
                }
            }
            else
            {
                GenRiftMonster(channel, GetMonsterLvl(channel) + 1);
                Thread.Sleep(100);
                if ((GetMonsterLvl(channel) % 5) == 0)
                {
                    Main.client.SendMessage(channel, "/me : [RIFT] Level up!! (Lvl " + GetMonsterLvl(channel) + ") Loot Chest has levelled up! (Lvl " + (GetMonsterLvl(channel) / 5) + ")");
                }
                else
                {
                    Main.client.SendMessage(channel, "/me : [RIFT] Level up!! (Lvl " + GetMonsterLvl(channel) + ")");
                }
            }
        }

        private void WriteMHP(string channel, int AttackDmg)
        {
            lock (Main._lockrift)
            {
                XDocument Rift = XDocument.Load(@"Data\Rift.xml");
                XElement M = Rift.Element("rift").Element("Channel." + channel).Element(channel + ".Monster");
                int MHP = Convert.ToInt32(M.Attribute("HP").Value);
                M.SetAttributeValue("HP", MHP - AttackDmg);
                Rift.Save(@"Data\Rift.xml");
            }
        }

        private void WritePlayerHP(string channel, string user, int AttackDmg)
        {
            lock (Main._lockrift)
            {
                XDocument Rift = XDocument.Load(@"Data\Rift.xml");
                XElement P = Rift.Element("rift").Element("Channel." + channel).Element(channel + ".Players").Element(channel + "." + user);
                int PHP = Convert.ToInt32(P.Attribute("HP").Value);
                P.SetAttributeValue("HP", PHP - AttackDmg);
                Rift.Save(@"Data\Rift.xml");
            }
        }

        private void TryHeal(string channel, string user)
        {
            if (random.Next(1, 6) == 4)
            {
                lock (Main._lockrift)
                {
                    XDocument Rift = XDocument.Load(@"Data\Rift.xml");
                    XElement P = Rift.Element("rift").Element("Channel." + channel).Element(channel + ".Players").Element(channel + "." + user);
                    int PHP = Convert.ToInt32(P.Attribute("HP").Value);
                    double Heals;
                    if (PHP + (Math.Floor(Convert.ToDouble(adventure.GetPlayerHP(channel, user)) / 5)) > adventure.GetPlayerHP(channel, user))
                    {
                        Heals = adventure.GetPlayerHP(channel, user);
                    }
                    else
                    {
                        Heals = (PHP + Math.Floor(Convert.ToDouble(adventure.GetPlayerHP(channel, user)) / 5));
                    }
                    P.SetAttributeValue("HP", (Heals));
                    Rift.Save(@"Data\Rift.xml");
                    Main.client.SendWhisper(user, "[HEAL] " + user.ToLower() + "'s " + adventure.GetRing(channel, user) + " has healed them, they are now at (" + Heals + ") HP.");
                }
            }
        }

        private void WriteAttackDmg(string channel, string user, int AttackDmg)
        {
            lock (Main._lockrift)
            {
                XDocument Rift = XDocument.Load(@"Data\Rift.xml");
                XElement P = Rift.Element("rift").Element("Channel." + channel).Element(channel + ".Players").Element(channel + "." + user);
                int CurDmg = Convert.ToInt32(P.Attribute("Dmg").Value);
                int TotDmg = 0;
                try
                {
                    TotDmg = Convert.ToInt32(P.Attribute("TotalDmg").Value);
                }
                catch
                {

                }
                P.SetAttributeValue("Dmg", CurDmg + AttackDmg);
                P.SetAttributeValue("TotalDmg", TotDmg + AttackDmg);
                Rift.Save(@"Data\Rift.xml");
            }
        }

        private void EndPlayer(string channel, string user)
        {
            XDocument Rift = XDocument.Load(@"Data\Rift.xml");
            XElement P = Rift.Element("rift").Element("Channel." + channel).Element(channel + ".Players").Element(channel + "." + user);
            lock (Main._lockrift)
            {
                P.SetAttributeValue("Playing", "No");
                Rift.Save(@"Data\Rift.xml");
            }
            XElement Ch = Rift.Element("rift").Element("Channel." + channel).Element(channel + ".Monster");
            int CurDmg = Convert.ToInt32(P.Attribute("Dmg").Value);
            Main.client.SendMessage(channel, "/me : [RIFT] " + user.ToLower() + " died at Lvl (" + GetMonsterLvl(channel) + ") and did " + CurDmg + " dmg.");
            string Mon = Ch.Attribute("Monster").Value;
            string Lvl = Ch.Attribute("MLvl").Value;
            string HP = Ch.Attribute("HP").Value;
            string MHP = Ch.Attribute("MHP").Value;
            Thread.Sleep(2000);
            Main.client.SendMessage(channel, "/me : [RIFT] Current Lvl : (" + Lvl + "). " + Mon + " has (" + HP + "/" + MHP + ") HP Remaining");
        }

        private string MakeLoot(string channel, string user, int lvl)
        {
            string Loot = null;
            if (lvl == 1)
            {
                Loot = adventure.MakeLoot(channel, user);
            }
            else if (lvl == 2)
            {
                int Chance1 = random.Next(101);
                int Chance2 = random.Next(101);
                string LootPrefix;
                string LootSuffix;
                if (Chance1 <= 40)
                {
                    LootPrefix = "Wooden ";
                }
                else if (Chance1 <= 72)
                {
                    LootPrefix = "Steel ";
                }
                else if (Chance1 <= 96)
                {
                    LootPrefix = "Moonstone ";
                }
                else
                {
                    LootPrefix = "Fire ";
                }
                string[] Type = { "Sword", "Helm", "Chest", "Legs", "Boots" };
                string LootType = Type[random.Next(Type.Length)];
                if (Chance2 <= 60)
                {
                    LootSuffix = null;
                }
                else if (Chance2 <= 65)
                {
                    LootSuffix = " of Eridin";
                }
                else if (Chance2 <= 80)
                {
                    LootSuffix = " of Brahma";
                }
                else if (Chance2 <= 97)
                {
                    LootSuffix = " of Night and Day";
                }
                else
                {
                    LootSuffix = " of Destiny 2.0";
                }
                Loot = LootPrefix + LootType + LootSuffix;
            }
            else if (lvl == 3)
            {
                Loot = adventure.MakeHardLoot(channel, user);
            }
            else if (lvl < 6)
            {
                if (random.Next(1, 4) == 3)
                {
                    Loot = adventure.MakeRing(Convert.ToInt32(Math.Ceiling(lvl * 1.5)));
                }
                else
                {
                    Loot = adventure.MakeRaidLoot(channel, user, Convert.ToInt32(Math.Ceiling(lvl * 1.5)));
                }
            }
            else
            {
                if (random.Next(1, 5) == 3)
                {
                    Loot = adventure.MakeRing(Convert.ToInt32(Math.Ceiling(lvl * 4.5)));
                }
                else
                {
                    while (Loot == null || Loot.Split(' ').Length < 3)
                    {
                        Loot = adventure.MakeRaidLoot(channel, user, Convert.ToInt32(Math.Ceiling(lvl * 2.5)));
                    }
                }
            }
            return Loot;
        }

        public void GiveShards(string channel, string user)
        {
             
            CheckMatsUser(channel, user);
            string Loot = MakeMats(1);
            int Qty = Convert.ToInt32(Loot.Substring(0, Loot.IndexOf(' ')));
            string Type = Loot.Substring(Loot.IndexOf(' ') + 1);
                lock (Main._lockloot)
                {
                    XDocument loot = XDocument.Load(@"Data\Loot.xml");
                    XElement P1 = loot.Element("loot").Element(Main.GetTopic(channel, user)).Element("Materials");
                    int Cur = 0;
                    try
                    {
                        Cur = Convert.ToInt32(P1.Attribute(Type).Value);
                    }
                    catch
                    {
                    }
                    P1.SetAttributeValue(Type, Cur + Qty);
                    Main.client.SendMessage(channel, "/me : BOP BOP BOP BOP Nice bopping. You got " + Qty + " " + Type + " shards! BOP BOP BOP BOP");
                    loot.Save(@"Data\Loot.xml");
            }
        }

        private string MakeMats(int lvl)
        {
            string[] List = { "Steel", "Steel", "Steel", "Moonstone", "Moonstone", "Fire", "Enchanted" };
            string Type = null;
            int Qty = 0;
            if (lvl < 6)
            {
                Type = List[random.Next(List.Length)];
                Qty = lvl * random.Next(3, 8);
            }
            else
            {
                while (Type == null || Type == "Steel" || Type == "Moonstone")
                {
                    Type = List[random.Next(List.Length)];
                    Qty = lvl * random.Next(4, 10);
                }
            }
            return Qty + " " + Type;
        }

        private void GiveLoot(string channel, int lvl)
        {
            Shop shop = new Shop();
            XDocument Rift = XDocument.Load(@"Data\Rift.xml");
            XElement P = Rift.Element("rift").Element("Channel." + channel).Element(channel + ".Players");
            string user;
            foreach (XElement ele in P.Elements())
            {
                if (lvl > 3)
                {
                    user = ele.Name.ToString().Substring(ele.Name.ToString().IndexOf('.') + 1);
                    adventure.CheckLootUser(channel, user);
                    adventure.CheckRingUser(channel, user);
                    if (random.Next(1, 6) < 4)
                    {
                        string Loot = MakeLoot(channel, user, lvl);
                        lock (Main._lockloot)
                        {
                            if (Main.LookUpLvl(channel, user) > 80)
                            {
                                if (!Loot.Contains("Shards"))
                                {
                                    while (Loot.Contains("Steel") || Loot.Contains("Moonstone"))
                                    {
                                        Loot = MakeLoot(channel, user, lvl);
                                    }
                                }
                            }
                            XDocument loot = XDocument.Load(@"Data\Loot.xml");
                            XElement P1 = loot.Element("loot").Element(Main.GetTopic(channel, user));
                            if (Loot.Contains("Ring"))
                            {
                                P1 = P1.Element("rings");
                                if (!adventure.IsRingBagFull(channel, user))
                                {
                                    P1.SetAttributeValue(adventure.GetNextRingLootSlot(channel, user), Loot);
                                    Main.client.SendWhisper(user, "[RIFT] You received " + Loot + " from the Lvl " + lvl + " chest.");
                                }
                                else
                                {
                                    Main.client.SendWhisper(user, "[RIFT] You couldnt carry your loot home and had to leave it behind :(");
                                }
                            }
                            else
                            {
                                if (!shop.IsBagFull(channel, user))
                                {
                                    P1.SetAttributeValue(adventure.GetNextLootSlot(channel, user), Loot);
                                    Main.client.SendWhisper(user, "[RIFT] You received " + Loot + " from the Lvl " + lvl + " chest.");
                                }
                                else
                                {
                                    Main.client.SendWhisper(user, "[RIFT] You couldnt carry your loot home and had to leave it behind :(");
                                }
                            }
                            loot.Save(@"Data\Loot.xml");
                        }
                    }
                    else
                    {
                        CheckMatsUser(channel, user);
                        string Loot = MakeMats(Convert.ToInt32(Math.Floor(lvl * 1.5)));
                        int Qty = Convert.ToInt32(Loot.Substring(0, Loot.IndexOf(' ')));
                        string Type = Loot.Substring(Loot.IndexOf(' ') + 1);
                        lock (Main._lockloot)
                        {
                            XDocument loot = XDocument.Load(@"Data\Loot.xml");
                            XElement P1 = loot.Element("loot").Element(Main.GetTopic(channel, user)).Element("Materials");
                            int Cur = 0;
                            try
                            {
                                Cur = Convert.ToInt32(P1.Attribute(Type).Value);
                            }
                            catch
                            {
                            }
                            P1.SetAttributeValue(Type, Cur + Qty);
                            Main.client.SendWhisper(user, "[RIFT] You received " + Qty + " " + Type + " Shards from the Lvl " + lvl + " chest.");
                            loot.Save(@"Data\Loot.xml");
                        }
                    }
                }
                else
                {
                    if (random.Next(1, 3) == 2)
                    {
                        user = ele.Name.ToString().Substring(ele.Name.ToString().IndexOf('.') + 1);
                        adventure.CheckLootUser(channel, user);
                        if (random.Next(1, 6) < 5)
                        {
                            string Loot = MakeLoot(channel, user, lvl);
                            lock (Main._lockloot)
                            {
                                XDocument loot = XDocument.Load(@"Data\Loot.xml");
                                XElement P1 = loot.Element("loot").Element(Main.GetTopic(channel, user));
                                if (Loot.Contains("Ring"))
                                {
                                    P = P.Element("rings");
                                    if (!adventure.IsRingBagFull(channel, user))
                                    {
                                        P.SetAttributeValue(adventure.GetNextRingLootSlot(channel, user), Loot);
                                    }
                                    else
                                    {
                                        Main.client.SendWhisper(user, "[RIFT] You couldnt carry your loot home and had to leave it behind :(");
                                    }
                                }
                                else
                                {
                                    if (!shop.IsBagFull(channel, user))
                                    {
                                        P1.SetAttributeValue(adventure.GetNextLootSlot(channel, user), Loot);
                                        Main.client.SendWhisper(user, "[RIFT] You received " + Loot + " from the Lvl " + lvl + " chest.");
                                    }
                                    else
                                    {
                                        Main.client.SendWhisper(user, "[RIFT] You couldnt carry your loot home and had to leave it behind :(");
                                    }
                                }
                                loot.Save(@"Data\Loot.xml");
                            }
                        }
                        else
                        {
                            CheckMatsUser(channel, user);
                            string Loot = MakeMats(lvl);
                            int Qty = Convert.ToInt32(Loot.Substring(0, Loot.IndexOf(' ')));
                            string Type = Loot.Substring(Loot.IndexOf(' ') + 1);
                            lock (Main._lockloot)
                            {
                                XDocument loot = XDocument.Load(@"Data\Loot.xml");
                                XElement P1 = loot.Element("loot").Element(Main.GetTopic(channel, user)).Element("Materials");
                                int Cur = 0;
                                try
                                {
                                    Cur = Convert.ToInt32(P1.Attribute(Type).Value);
                                }
                                catch
                                {
                                }
                                P1.SetAttributeValue(Type, Cur + Qty);
                                Main.client.SendWhisper(user, "[RIFT] You received " + Qty + " " + Type + " Shards from the Lvl " + lvl + " chest.");
                                loot.Save(@"Data\Loot.xml");
                            }
                        }
                    }
                }
            }
        }

        private void GiveCoin(string channel, int lvl)
        {
            XDocument Rift = XDocument.Load(@"Data\Rift.xml");
            XElement P1 = Rift.Element("rift").Element("Channel." + channel).Element(channel + ".Players");
            int players = P1.Elements().Count();
            List<int> listNumbers = new List<int>();
            int number;
            for (int i = 0; i < (players - 1); i++)
            {
                do
                {
                    number = random.Next(1, (lvl * 10) * players);
                } while (listNumbers.Contains(number));
                listNumbers.Add(number);
            }
            listNumbers.Add(0);
            listNumbers.Add((lvl * 10) * players);
            listNumbers.Sort();
            int index = 0;
            string user;
            string msg = "/me : [RIFT] ";
            foreach (XElement ele in P1.Elements())
            {
                user = ele.Name.ToString().Substring(ele.Name.ToString().IndexOf('.') + 1);
                int Coins = Math.Abs(listNumbers[index] - listNumbers[index + 1]);
                adventure.CheckLootUser(channel, user);
                msg = msg + user.ToLower() + " received: " + Coins + " Gold Coins, ";
                Main.client.SendWhisper(user, "You received " + Coins + " Gold Coins from the Rift.");
                lock (Main._lockloot)
                {
                    XDocument loot = XDocument.Load(@"Data\Loot.xml");
                    XElement P = loot.Element("loot").Element(Main.GetTopic(channel, user));
                    if (adventure.GetLootSlot1(channel, user) != null && adventure.GetLootSlot1(channel, user).Contains("Coins"))
                    {
                        P.SetAttributeValue("Slot1", (Coins + Convert.ToInt32(adventure.GetLootSlot1(channel, user).Substring(0, adventure.GetLootSlot1(channel, user).IndexOf(" ")))) + " Gold Coins");
                    }
                    else if (adventure.GetLootSlot2(channel, user) != null && adventure.GetLootSlot2(channel, user).Contains("Coins"))
                    {
                        P.SetAttributeValue("Slot2", (Coins + Convert.ToInt32(adventure.GetLootSlot2(channel, user).Substring(0, adventure.GetLootSlot2(channel, user).IndexOf(" ")))) + " Gold Coins");
                    }
                    else if (adventure.GetLootSlot3(channel, user) != null && adventure.GetLootSlot3(channel, user).Contains("Coins"))
                    {
                        P.SetAttributeValue("Slot3", (Coins + Convert.ToInt32(adventure.GetLootSlot3(channel, user).Substring(0, adventure.GetLootSlot3(channel, user).IndexOf(" ")))) + " Gold Coins");
                    }
                    else if (adventure.GetLootSlot4(channel, user) != null && adventure.GetLootSlot4(channel, user).Contains("Coins"))
                    {
                        P.SetAttributeValue("Slot4", (Coins + Convert.ToInt32(adventure.GetLootSlot4(channel, user).Substring(0, adventure.GetLootSlot4(channel, user).IndexOf(" ")))) + " Gold Coins");
                    }
                    else if (adventure.GetLootSlot5(channel, user) != null && adventure.GetLootSlot5(channel, user).Contains("Coins"))
                    {
                        P.SetAttributeValue("Slot5", (Coins + Convert.ToInt32(adventure.GetLootSlot5(channel, user).Substring(0, adventure.GetLootSlot5(channel, user).IndexOf(" ")))) + " Gold Coins");
                    }
                    else if (adventure.GetLootSlot1(channel, user) == null)
                    {
                        P.SetAttributeValue("Slot1", Coins + " Gold Coins");
                    }
                    else if (adventure.GetLootSlot2(channel, user) == null)
                    {
                        P.SetAttributeValue("Slot2", Coins + " Gold Coins");
                    }
                    else if (adventure.GetLootSlot3(channel, user) == null)
                    {
                        P.SetAttributeValue("Slot3", Coins + " Gold Coins");
                    }
                    else if (adventure.GetLootSlot4(channel, user) == null)
                    {
                        P.SetAttributeValue("Slot4", Coins + " Gold Coins");
                    }
                    else if (adventure.GetLootSlot5(channel, user) == null)
                    {
                        P.SetAttributeValue("Slot5", Coins + " Gold Coins");
                    }
                    else
                    {
                        Main.client.SendMessage(channel, "/me : [LOOT] " + user.ToLower() + " couldnt carry their loot home and had to leave it behind :(");
                        Main.client.SendWhisper(user, "But your bag was full and had to drop them!");
                    }
                    loot.Save(@"Data\Loot.xml");
                }
                index++;
            }
            Main.client.SendMessage(channel, msg.TrimEnd(' ', ','));
        }

        public void EndRift(string channel)
        {
            XDocument Rift = XDocument.Load(@"Data\Rift.xml");
            XElement Ch = Rift.Element("rift").Element("Channel." + channel).Element(channel + ".Monster");
            int Lvl = Convert.ToInt32(Ch.Attribute("MLvl").Value);
            Main.client.SendMessage(channel, "/me : [RIFT] Rift is closing. Well done, you reached lvl " + Lvl + " (chest lvl " + Math.Floor(Lvl / 5.0) + "). Spoils will now be distributed...");
            if (Math.Floor(Lvl / 5.0) > 0)
            {
                GiveLoot(channel, Convert.ToInt32(Math.Floor(Lvl / 5.0)));
                GiveCoin(channel, Convert.ToInt32(Math.Floor(Lvl / 5.0)));
            }
            ClearRift(channel);
        }

        private void ClearRift(string channel)
        {
            try
            {
                lock (Main._lockrift)
                {
                    XDocument Rift = XDocument.Load(@"Data\Rift.xml");
                    XElement C = Rift.Element("rift").Element("Channel." + channel);
                    C.RemoveAll();
                    Rift.Save(@"Data\Rift.xml");
                }
            }
            catch
            {

            }
            CheckChannel(channel);
            lock (Main._lockrift)
            {
                XDocument Rift2 = XDocument.Load(@"Data\Rift.xml");
                XElement C2 = Rift2.Element("rift").Element("Channel." + channel);
                C2.SetAttributeValue("Status", "Off");
                Rift2.Save(@"Data\Rift.xml");
            }
        }

    }
}