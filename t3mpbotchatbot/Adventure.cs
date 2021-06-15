using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using TwitchLib.Client;

namespace t3mpbotchatbot
{

    public class Adventure
    {
        Random random = new Random(Guid.NewGuid().GetHashCode());
        public Dictionary<string, List<string>> PlayerList = new Dictionary<string, List<string>>();
        //public List<string> PlayerList = new List<string>();

        private void CheckChannel(string channel)
        {
            lock (Main._lockadv)
            {
                XDocument Adv = XDocument.Load(@"Data\Adventure.xml");
                var ele = Adv.Element("adventure").Elements("Channel." + channel).SingleOrDefault();
                if (ele == null)
                {
                    Adv.Element("adventure").Add(new XElement("Channel." + channel));
                    Adv.Save(@"Data\Adventure.xml");
                }
            }
        }

        private void CheckUser(string channel, string user)
        {
            lock (Main._lockadv)
            {
                XDocument Adv = XDocument.Load(@"Data\Adventure.xml");
                XElement P = Adv.Element("adventure").Element("Channel." + channel);
                var ele2 = P.Elements(channel + "." + user).SingleOrDefault();
                if (ele2 == null)
                {
                    P.Add(new XElement(channel + "." + user,
                        new XAttribute("TotalDmg", 0)));
                    Adv.Save(@"Data\Adventure.xml");
                }
            }
        }

        public void CheckLootUser(string channel, string user)
        {
            lock (Main._lockloot)
            {
                XDocument Loot = XDocument.Load(@"Data\Loot.xml");
                XElement P = Loot.Element("loot");
                var ele2 = P.Elements(Main.GetTopic(channel, user)).SingleOrDefault();
                if (ele2 == null)
                {
                    P.Add(new XElement(Main.GetTopic(channel, user)));
                    Loot.Save(@"Data\Loot.xml");
                }
            }
            CheckRingUser(channel, user);
        }

        public void CheckRingUser(string channel, string user)
        {
            lock (Main._lockloot)
            {
                XDocument Loot = XDocument.Load(@"Data\Loot.xml");
                XElement P = Loot.Element("loot").Element(Main.GetTopic(channel, user));
                var ele2 = P.Elements("rings").SingleOrDefault();
                if (ele2 == null)
                {
                    P.Add(new XElement("rings"));
                    Loot.Save(@"Data\Loot.xml");
                }
            }
        }

        private void CheckMonster(string channel)
        {
            lock (Main._lockadv)
            {
                XDocument Adv = XDocument.Load(@"Data\Adventure.xml");
                XElement P = Adv.Element("adventure").Element("Channel." + channel);
                var ele2 = P.Elements(channel + ".Monster").SingleOrDefault();
                if (ele2 == null)
                {
                    P.Add(new XElement(channel + ".Monster"));
                    Adv.Save(@"Data\Adventure.xml");
                }
            }
        }

        public void SetHard(string channel)
        {
            CheckChannel(channel);
            lock (Main._lockadv)
            {
                XDocument Adv = XDocument.Load(@"Data\Adventure.xml");
                XElement P2 = Adv.Element("adventure").Element("Channel." + channel);
                P2.SetAttributeValue("Difficulty", "Hard");
                Adv.Save(@"Data\Adventure.xml");
            }
        }

        public void SetRaid(string channel)
        {
            CheckChannel(channel);
            lock (Main._lockadv)
            {
                XDocument Adv = XDocument.Load(@"Data\Adventure.xml");
                XElement P2 = Adv.Element("adventure").Element("Channel." + channel);
                P2.SetAttributeValue("Difficulty", "Raid");
                Adv.Save(@"Data\Adventure.xml");
            }
        }

        public void Set150(string channel)
        {
            CheckChannel(channel);
            lock (Main._lockadv)
            {
                XDocument Adv = XDocument.Load(@"Data\Adventure.xml");
                XElement P2 = Adv.Element("adventure").Element("Channel." + channel);
                P2.SetAttributeValue("Difficulty", "150");
                Adv.Save(@"Data\Adventure.xml");
            }
        }

        public bool IsHard(string channel)
        {
            CheckChannel(channel);
            lock (Main._lockadv)
            {
                XDocument Adv = XDocument.Load(@"Data\Adventure.xml");
                XElement P2 = Adv.Element("adventure").Element("Channel." + channel);
                bool IsHard = false;
                foreach (XAttribute atr in P2.Attributes())
                {
                    if (atr.Name == "Difficulty" && atr.Value == "Hard")
                    {
                        IsHard = true;
                    }
                }
                return IsHard;
            }
        }

        public bool IsRaid(string channel)
        {
            CheckChannel(channel);
            lock (Main._lockadv)
            {
                XDocument Adv = XDocument.Load(@"Data\Adventure.xml");
                XElement P2 = Adv.Element("adventure").Element("Channel." + channel);
                bool IsRaid = false;
                foreach (XAttribute atr in P2.Attributes())
                {
                    if (atr.Name == "Difficulty" && atr.Value == "Raid")
                    {
                        IsRaid = true;
                    }
                }
                return IsRaid;
            }
        }

        public bool Is150(string channel)
        {
            CheckChannel(channel);
            lock (Main._lockadv)
            {
                XDocument Adv = XDocument.Load(@"Data\Adventure.xml");
                XElement P2 = Adv.Element("adventure").Element("Channel." + channel);
                bool Is150 = false;
                foreach (XAttribute atr in P2.Attributes())
                {
                    if (atr.Name == "Difficulty" && atr.Value == "150")
                    {
                        Is150 = true;
                    }
                }
                return Is150;
            }
        }

        public void AddPlayer(string channel, string user)
        {
            CheckChannel(channel);
            CheckUser(channel, user);
            lock (Main._lockadv)
            {
                XDocument Adv = XDocument.Load(@"Data\Adventure.xml");
                XElement P2 = Adv.Element("adventure").Element("Channel." + channel).Element(channel + "." + user);
                P2.SetAttributeValue("HP", GetPlayerHP(channel, user).ToString());
                Adv.Save(@"Data\Adventure.xml");
            }
            //PlayerList.Add(user);
            if (PlayerList.ContainsKey(channel))
            {
                PlayerList[channel].Add(user);
            }
            else
            {
                PlayerList[channel] = new List<string> { user };
            }
        }

        public int GetPlayerHP(string channel, string user)
        {
            lock (Main._lockstats)
            {
                XElement Stats = XElement.Load(@"Data\Stats.xml");
                int HP = 0;
                foreach (XElement ele in Stats.Descendants(Main.GetTopic(channel, user)))
                {
                    HP = Convert.ToInt32(ele.Attribute("HP").Value.ToString());
                }
                return HP;
            }
        }

        public int GetPlayerArc(string channel, string user)
        {
            lock (Main._lockstats)
            {
                XElement Stats = XElement.Load(@"Data\Stats.xml");
                int Arc = 0;
                foreach (XElement ele in Stats.Descendants(Main.GetTopic(channel, user)))
                {
                    Arc = Convert.ToInt32(ele.Attribute("Archery").Value);
                }
                return Arc;
            }
        }

        public int GetPlayerStr(string channel, string user)
        {
            lock (Main._lockstats)
            {
                XElement Stats = XElement.Load(@"Data\Stats.xml");
                int Str = 0;
                foreach (XElement ele in Stats.Descendants(Main.GetTopic(channel, user)))
                {
                    Str = Convert.ToInt32(ele.Attribute("Strength").Value);
                }
                return Str;
            }
        }

        public int GetPlayerEnd(string channel, string user)
        {
            lock (Main._lockstats)
            {
                XElement Stats = XElement.Load(@"Data\Stats.xml");
                int End = 0;
                foreach (XElement ele in Stats.Descendants(Main.GetTopic(channel, user)))
                {
                    End = Convert.ToInt32(ele.Attribute("Endurance").Value);
                }
                return End;
            }
        }

        public int GetPlayerInt(string channel, string user)
        {
            lock (Main._lockstats)
            {
                XElement Stats = XElement.Load(@"Data\Stats.xml");
                int Int = 0;
                foreach (XElement ele in Stats.Descendants(Main.GetTopic(channel, user)))
                {
                    Int = Convert.ToInt32(ele.Attribute("Intelligence").Value);
                }
                return Int;
            }
        }

        public int GetPlayerDex(string channel, string user)
        {
            lock (Main._lockstats)
            {
                XElement Stats = XElement.Load(@"Data\Stats.xml");
                int Dex = 0;
                foreach (XElement ele in Stats.Descendants(Main.GetTopic(channel, user)))
                {
                    Dex = Convert.ToInt32(ele.Attribute("Dexterity").Value);
                }
                return Dex;
            }
        }

        public int GetPlayerLoo(string channel, string user)
        {
            lock (Main._lockstats)
            {
                XElement Stats = XElement.Load(@"Data\Stats.xml");
                int Loo = 0;
                foreach (XElement ele in Stats.Descendants(Main.GetTopic(channel, user)))
                {
                    Loo = Convert.ToInt32(ele.Attribute("Looting").Value);
                }
                return Loo;
            }
        }

        public int GetPlayerFai(string channel, string user)
        {
            lock (Main._lockstats)
            {
                XElement Stats = XElement.Load(@"Data\Stats.xml");
                int Fai = 0;
                foreach (XElement ele in Stats.Descendants(Main.GetTopic(channel, user)))
                {
                    Fai = Convert.ToInt32(ele.Attribute("Faith").Value);
                }
                return Fai;
            }
        }

        public int GetPlayerSpe(string channel, string user)
        {
            lock (Main._lockstats)
            {
                XElement Stats = XElement.Load(@"Data\Stats.xml");
                int Spe = 0;
                foreach (XElement ele in Stats.Descendants(Main.GetTopic(channel, user)))
                {
                    Spe = Convert.ToInt32(ele.Attribute("Speed").Value);
                }
                return Spe;
            }
        }

        private int GetMonsterLvl(string channel)
        {
            lock (Main._lockadv)
            {
                XElement Adv = XElement.Load(@"Data\Adventure.xml");
                int MLvl = 0;
                foreach (XElement ele in Adv.Descendants(channel + ".Monster"))
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

        private int GetMonsterHP(string channel)
        {
            lock (Main._lockadv)
            {
                XElement Adv = XElement.Load(@"Data\Adventure.xml");
                int MHP = 0;
                foreach (XElement ele in Adv.Descendants(channel + ".Monster"))
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

        private int GetMonsterDex(string channel)
        {
            lock (Main._lockadv)
            {
                XElement Adv = XElement.Load(@"Data\Adventure.xml");
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
            lock (Main._lockadv)
            {
                XElement Adv = XElement.Load(@"Data\Adventure.xml");
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
            lock (Main._lockadv)
            {
                XElement Adv = XElement.Load(@"Data\Adventure.xml");
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
            lock (Main._lockadv)
            {
                XElement Adv = XElement.Load(@"Data\Adventure.xml");
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

        private int GetAdvPlayerHP(string channel, string user)
        {
            lock (Main._lockadv)
            {
                XElement Adv = XElement.Load(@"Data\Adventure.xml");
                int PHP = 0;
                foreach (XElement ele in Adv.Descendants(channel + "." + user))
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

        private bool CheckDead(string channel, string user)
        {
            lock (Main._lockadv)
            {
                XElement Adv = XElement.Load(@"Data\Adventure.xml");
                bool Dead = false;
                foreach (XElement ele in Adv.Descendants(channel + "." + user))
                {
                    try
                    {
                        Dead = Convert.ToBoolean(ele.Attribute("Dead").Value);
                    }
                    catch
                    {
                        Dead = false;
                    }
                }
                return Dead;
            }
        }

        private bool CheckAllDead(string channel)
        {
            bool AllDead = true;
            foreach (string plyr in PlayerList[channel])
            {
                if (!CheckDead(channel, plyr))
                {
                    AllDead = false;
                }
            }
            return AllDead;
        }

        private void GenMonster(string channel, string user)
        {
            string Monster = MakeMonster();
            string MLvl = GenMonsterLvl(channel, user);
            string MHP = GenMonsterHP(channel, user);
            string MStr = GenMonsterStr(channel, user);
            string MEnd = GenMonsterEnd(channel, user);
            string MInt = GenMonsterInt(channel, user);
            string MDex = GenMonsterDex(channel, user);
            if (IsHard(channel))
            {
                Monster = MakeHardMonster();
                MHP = GenHardMonsterHP(channel, user);
                MStr = GenHardMonsterStr(channel, user);
                MEnd = GenHardMonsterEnd(channel, user);
                MInt = GenHardMonsterInt(channel, user);
                MDex = GenHardMonsterDex(channel, user);
            }
            else if (Is150(channel))
            {
                Monster = Make150Monster();
                MHP = Gen150MonsterHP(channel, user);
                MStr = Gen150MonsterStr(channel, user);
                MEnd = Gen150MonsterEnd(channel, user);
                MInt = Gen150MonsterInt(channel, user);
                MDex = Gen150MonsterDex(channel, user);
            }
            CheckChannel(channel);
            CheckMonster(channel);
            lock (Main._lockadv)
            {
                XDocument Adv = XDocument.Load(@"Data\Adventure.xml");
                XElement monster = Adv.Element("adventure").Element("Channel." + channel).Element(channel + ".Monster");
                monster.SetAttributeValue("Monster", Monster);
                monster.SetAttributeValue("MLvl", MLvl);
                monster.SetAttributeValue("MHP", MHP);
                monster.SetAttributeValue("MStr", MStr);
                monster.SetAttributeValue("MEnd", MEnd);
                monster.SetAttributeValue("MInt", MInt);
                monster.SetAttributeValue("MDex", MDex);
                monster.SetAttributeValue("HP", MHP);
                Adv.Save(@"Data\Adventure.xml");
            }
            Main.client.SendMessage(channel, "/me : " + Monster + " has arisen with the stats: LVL(" + MLvl + ") HP (" + MHP + ") Strength (" + MStr + ") Endurance (" + MEnd + ") Intelligence (" + MInt + ") Dexterity (" + MDex + ")");
        }

        private void GenRaidMonster(string channel, int level)
        {
            string Monster = MakeRaidMonster(channel, level);
            string MLvl = level.ToString();
            string MHP = (level * 150).ToString();
            string MStr = Math.Ceiling((level + 5) / 1.2).ToString();
            string MEnd = Math.Ceiling((level + 5) / 0.9).ToString();
            string MInt = Math.Ceiling((level + 4) / 1.7).ToString();
            string MDex = Math.Ceiling((level + 4) / 1.3).ToString();
            CheckChannel(channel);
            CheckMonster(channel);
            lock (Main._lockadv)
            {
                XDocument Adv = XDocument.Load(@"Data\Adventure.xml");
                XElement monster = Adv.Element("adventure").Element("Channel." + channel).Element(channel + ".Monster");
                monster.SetAttributeValue("Monster", Monster);
                monster.SetAttributeValue("MLvl", MLvl);
                monster.SetAttributeValue("MHP", MHP);
                monster.SetAttributeValue("MStr", MStr);
                monster.SetAttributeValue("MEnd", MEnd);
                monster.SetAttributeValue("MInt", MInt);
                monster.SetAttributeValue("MDex", MDex);
                monster.SetAttributeValue("HP", MHP);
                Adv.Save(@"Data\Adventure.xml");
            }
            Main.client.SendMessage(channel, "/me : " + Monster + " has arisen with the stats: LVL(" + MLvl + ") HP (" + MHP + ") Strength (" + MStr + ") Endurance (" + MEnd + ") Intelligence (" + MInt + ") Dexterity (" + MDex + ")");
        }

        private string MakeMonster()
        {
            string[] prefixes = { "Kryton", "Pilth", "Darius", "Martel", "Damon", "Lucas", "Stacy", "Sheeva" };
            int indexPre = random.Next(prefixes.Length);
            string[] types = { "Orc", "Goblin", "Dragon", "Master", "Troll", "Elvern", "Zombie", "Skeleton" };
            int indexTyp = random.Next(types.Length);
            string[] suffixes = { "Warrior", "King", "Pilgrim", "Tank", "Champion", "Soldier", "Bruiser", "Warlord" };
            int indexSuf = random.Next(suffixes.Length);
            string prefix = prefixes[indexPre];
            string type = " The " + types[indexTyp] + " ";
            string suffix = suffixes[indexSuf];
            return prefix + type + suffix;
        }

        private string MakeHardMonster()
        {
            string[] prefixes = { "Upanga", "Exulio", "Mark", "Aribid", "Jupiter", "Cassidy", "Kindo", "Steph" };
            int indexPre = random.Next(prefixes.Length);
            string[] types = { "Giant", "Undead", "Dragon Master", "Goliath", "Halfling", "Infected", "Lizard", "Centaur" };
            int indexTyp = random.Next(types.Length);
            string[] suffixes = { "Warrior", "King", "Pilgrim", "Tank", "Champion", "Soldier", "Bruiser", "Warlord" };
            int indexSuf = random.Next(suffixes.Length);
            string prefix = prefixes[indexPre];
            string type = " The " + types[indexTyp] + " ";
            string suffix = suffixes[indexSuf];
            return prefix + type + suffix;
        }

        private string Make150Monster()
        {
            string[] prefixes = { "Golem", "Aaron", "Hellstone", "Astro", "Gareth", "Lucious", "Ken", "Hentimolanpium" };
            int indexPre = random.Next(prefixes.Length);
            string[] types = { "Diamond", "Gold", "Angry", "Psycho", "Raging", "Lazy", "Pink", "Fire" };
            int indexTyp = random.Next(types.Length);
            string[] suffixes = { "Fighter", "Kickboxer", "Walker", "Midget Thrower", "Archer", "Hacker", "Bull Fighter", "Eater" };
            int indexSuf = random.Next(suffixes.Length);
            string prefix = prefixes[indexPre];
            string type = " The " + types[indexTyp] + " ";
            string suffix = suffixes[indexSuf];
            return prefix + type + suffix;
        }

        private string MakeRaidMonster(string channel, int level)
        {
            List<string> monster = new List<string>();
            if (level <= 25)
            {
                string[] monsters = { "Dirge", "Mothrakk", "Mewtwo", "Shadow Enchantress" };
                monster.AddRange(monsters);
            }
            else if (level <= 40)
            {
                string[] monsters = { "Emerald Nightmare", "Bowser", "Gigantor", "Grim Reaper" };
                monster.AddRange(monsters);
            }
            else if (level <= 65)
            {
                string[] monsters = { "Artorias", "Terramorphous the Invincible", "Dracula", "Handsome Jack" };
                monster.AddRange(monsters);
            }
            else
            {
                string[] monsters = { "Skolas", "Psycho Mantis", "Nemesis", channel.ToLower() };
                monster.AddRange(monsters);
            }
            return monster[random.Next(monster.Count)];
        }

        private string GenMonsterLvl(string channel, string user)
        {
            int Lvl = Main.LookUpLvl(channel, user);
            double[] values = { 0.9, 0.95, 1, 1.05, 1.1 };
            int index = random.Next(values.Length);
            string MLvl = Math.Floor(Lvl * values[index]).ToString();
            return MLvl;
        }

        private string GenMonsterHP(string channel, string user)
        {
            int HP = GetPlayerHP(channel, user);
            double[] values = { 4, 4.5, 5, 5.5, 6 };
            int index = random.Next(values.Length);
            string MHP = Math.Floor(HP * values[index]).ToString();
            return MHP;
        }

        private string GenMonsterStr(string channel, string user)
        {
            int Str = GetPlayerStr(channel, user);
            if (GetWeapon(channel, user).Contains("Bow"))
            {
                Str = GetPlayerArc(channel, user);
            }
            double[] values = { 0.3, 0.4, 0.5, 0.6 };
            int index = random.Next(values.Length);
            string MStr = Math.Floor(Str * values[index]).ToString();
            return MStr;
        }

        private string GenMonsterEnd(string channel, string user)
        {
            int End = GetPlayerEnd(channel, user);
            double[] values = { 0.5, 0.6, 0.7, 0.8, 0.9, 1, 1.1, 1.2, 1.3, 1.4, 1.5 };
            int index = random.Next(values.Length);
            string MEnd = Math.Floor(End * values[index]).ToString();
            return MEnd;
        }

        private string GenMonsterInt(string channel, string user)
        {
            int Int = GetPlayerInt(channel, user);
            double[] values = { 0.2, 0.3, 0.4 };
            int index = random.Next(values.Length);
            string MInt = Math.Floor(Int * values[index]).ToString();
            return MInt;
        }

        private string GenMonsterDex(string channel, string user)
        {
            int Dex = GetPlayerDex(channel, user);
            double[] values = { 0.6, 0.7, 0.8, 0.9 };
            int index = random.Next(values.Length);
            string MDex = Math.Floor(Dex * values[index]).ToString();
            return MDex;
        }

        private string GenHardMonsterHP(string channel, string user)
        {
            int HP = GetPlayerHP(channel, user);
            double[] values = { 6, 7, 8, 9, 10 };
            int index = random.Next(values.Length);
            string MHP = Math.Floor(HP * values[index]).ToString();
            return MHP;
        }

        private string GenHardMonsterStr(string channel, string user)
        {
            int Str = GetPlayerStr(channel, user);
            if (GetWeapon(channel, user).Contains("Bow"))
            {
                Str = GetPlayerArc(channel, user);
            }
            double[] values = { 0.5, 0.7, 0.9, 1 };
            int index = random.Next(values.Length);
            string MStr = Math.Floor(Str * values[index]).ToString();
            return MStr;
        }

        private string GenHardMonsterEnd(string channel, string user)
        {
            int End = GetPlayerEnd(channel, user);
            double[] values = { 0.9, 1, 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 1.9 };
            int index = random.Next(values.Length);
            string MEnd = Math.Floor(End * values[index]).ToString();
            return MEnd;
        }

        private string GenHardMonsterInt(string channel, string user)
        {
            int Int = GetPlayerInt(channel, user);
            double[] values = { 0.5, 0.6, 0.7 };
            int index = random.Next(values.Length);
            string MInt = Math.Floor(Int * values[index]).ToString();
            return MInt;
        }

        private string GenHardMonsterDex(string channel, string user)
        {
            int Dex = GetPlayerDex(channel, user);
            double[] values = { 1, 1.1, 1.2, 1.3 };
            int index = random.Next(values.Length);
            string MDex = Math.Floor(Dex * values[index]).ToString();
            return MDex;
        }

        private string Gen150MonsterHP(string channel, string user)
        {
            int HP = GetPlayerHP(channel, user);
            double[] values = { 8, 9, 10, 11, 12 };
            int index = random.Next(values.Length);
            string MHP = Math.Floor(HP * values[index]).ToString();
            return MHP;
        }

        private string Gen150MonsterStr(string channel, string user)
        {
            int Str = GetPlayerStr(channel, user);
            if (GetWeapon(channel, user).Contains("Bow"))
            {
                Str = GetPlayerArc(channel, user);
            }
            double[] values = { 0.8, 0.9, 1, 1.1 };
            int index = random.Next(values.Length);
            string MStr = Math.Floor(Str * values[index]).ToString();
            return MStr;
        }

        private string Gen150MonsterEnd(string channel, string user)
        {
            int End = GetPlayerEnd(channel, user);
            double[] values = { 0.5, 0.6, 0.7, 0.8, 0.9, 1, 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 1.9 };
            int index = random.Next(values.Length);
            string MEnd = Math.Floor(End * values[index]).ToString();
            return MEnd;
        }

        private string Gen150MonsterInt(string channel, string user)
        {
            int Int = GetPlayerInt(channel, user);
            double[] values = { 0.8, 0.9, 1 };
            int index = random.Next(values.Length);
            string MInt = Math.Floor(Int * values[index]).ToString();
            return MInt;
        }

        private string Gen150MonsterDex(string channel, string user)
        {
            int Dex = GetPlayerDex(channel, user);
            double[] values = { 0.7, 0.8, 0.9, 1, 1.1, 1.2, 1.3 };
            int index = random.Next(values.Length);
            string MDex = Math.Floor(Dex * values[index]).ToString();
            return MDex;
        }

        public void StartRaid(string channel, int level)
        {
            string player1 = PlayerList[channel][0];
            string player2;
            string player3;
            string player4;
            string player5;
            string player6;
            string player7;
            string player8;
            lock (Main._lockadv)
            {
                XDocument Adv = XDocument.Load(@"Data\Adventure.xml");
                XElement play = Adv.Element("adventure").Element("Channel." + channel);
                if (PlayerList[channel].Count() >= 2)
                {
                    player2 = PlayerList[channel][1];
                }
                else
                {
                    player2 = null;
                }
                if (PlayerList[channel].Count() >= 3)
                {
                    player3 = PlayerList[channel][2];
                }
                else
                {
                    player3 = null;
                }
                if (PlayerList[channel].Count() >= 4)
                {
                    player4 = PlayerList[channel][3];
                }
                else
                {
                    player4 = null;
                }
                if (PlayerList[channel].Count() >= 5)
                {
                    player5 = PlayerList[channel][4];
                }
                else
                {
                    player5 = null;
                }
                if (PlayerList[channel].Count() >= 6)
                {
                    player6 = PlayerList[channel][5];
                }
                else
                {
                    player6 = null;
                }
                if (PlayerList[channel].Count() >= 7)
                {
                    player7 = PlayerList[channel][6];
                }
                else
                {
                    player7 = null;
                }
                if (PlayerList[channel].Count() >= 8)
                {
                    player8 = PlayerList[channel][7];
                }
                else
                {
                    player8 = null;
                }
                play.Element(channel + "." + player1).SetAttributeValue("HP", GetPlayerHP(channel, player1));
                if (player2 != null)
                {
                    play.Element(channel + "." + player2).SetAttributeValue("HP", GetPlayerHP(channel, player2));
                }
                if (player3 != null)
                {
                    play.Element(channel + "." + player3).SetAttributeValue("HP", GetPlayerHP(channel, player3));
                }
                if (player4 != null)
                {
                    play.Element(channel + "." + player4).SetAttributeValue("HP", GetPlayerHP(channel, player4));
                }
                if (player5 != null)
                {
                    play.Element(channel + "." + player5).SetAttributeValue("HP", GetPlayerHP(channel, player5));
                }
                if (player6 != null)
                {
                    play.Element(channel + "." + player6).SetAttributeValue("HP", GetPlayerHP(channel, player6));
                }
                if (player7 != null)
                {
                    play.Element(channel + "." + player7).SetAttributeValue("HP", GetPlayerHP(channel, player7));
                }
                if (player8 != null)
                {
                    play.Element(channel + "." + player8).SetAttributeValue("HP", GetPlayerHP(channel, player8));
                }
                play.SetAttributeValue("Status", "Running");
                Adv.Save(@"Data\Adventure.xml");
            }
            System.Threading.Thread.Sleep(1000);
            GenRaidMonster(channel, level);
            System.Threading.Thread.Sleep(2000);
            Main.client.SendMessage(channel, "/me : Raid Started!");
            System.Threading.Thread.Sleep(3000);
            StartRaidAttack(channel, player1, player2, player3, player4, player5, player6, player7, player8);
        }

        public void StartAdventure(string channel, string user)
        {
            string player1 = PlayerList[channel][0];
            string player2;
            string player3;
            string player4;
            lock (Main._lockadv)
            {
                XDocument Adv = XDocument.Load(@"Data\Adventure.xml");
                XElement play = Adv.Element("adventure").Element("Channel." + channel);
                if (PlayerList[channel].Count() >= 2)
                {
                    player2 = PlayerList[channel][1];
                }
                else
                {
                    player2 = null;
                }
                if (PlayerList[channel].Count() >= 3)
                {
                    player3 = PlayerList[channel][2];
                }
                else
                {
                    player3 = null;
                }
                if (PlayerList[channel].Count() >= 4)
                {
                    player4 = PlayerList[channel][3];
                }
                else
                {
                    player4 = null;
                }
                play.Element(channel + "." + player1).SetAttributeValue("HP", GetPlayerHP(channel, player1));
                if (player2 != null)
                {
                    play.Element(channel + "." + player2).SetAttributeValue("HP", GetPlayerHP(channel, player2));
                }
                if (player3 != null)
                {
                    play.Element(channel + "." + player3).SetAttributeValue("HP", GetPlayerHP(channel, player3));
                }
                if (player4 != null)
                {
                    play.Element(channel + "." + player4).SetAttributeValue("HP", GetPlayerHP(channel, player4));
                }
                play.SetAttributeValue("Status", "Running");
                Adv.Save(@"Data\Adventure.xml");
            }
            System.Threading.Thread.Sleep(1000);
            GenMonster(channel, user);
            System.Threading.Thread.Sleep(2000);
            Main.client.SendMessage(channel, "/me : Adventure Started!");
            System.Threading.Thread.Sleep(3000);
            StartAttack(channel, player1, player2, player3, player4);
        }

        private void StartAttack(string channel, string player1, string player2, string player3, string player4)
        {
            int MHP = GetMonsterHP(channel);
            while (MHP > 0)
            {
                Console.WriteLine("Cycling Attack");
                DoAttack(channel, player1);
                System.Threading.Thread.Sleep(3000);
                if (PlayerList[channel].Count >= 2)
                {
                    DoAttack(channel, player2);
                    System.Threading.Thread.Sleep(3000);
                }
                if (PlayerList[channel].Count >= 3)
                {
                    DoAttack(channel, player3);
                    System.Threading.Thread.Sleep(3000);
                }
                if (PlayerList[channel].Count >= 4)
                {
                    DoAttack(channel, player4);
                    System.Threading.Thread.Sleep(3000);
                }
                DoMAttack(channel);
                System.Threading.Thread.Sleep(3000);
                if (CheckAllDead(channel))
                {
                    System.Threading.Thread.Sleep(1000);
                    Console.WriteLine("All is dead, breaking");
                    break;
                }
                MHP = GetMonsterHP(channel);
            }
            Console.WriteLine("Ending Adventure");
            EndAdventure(channel);
        }

        private void StartRaidAttack(string channel, string player1, string player2, string player3, string player4, string player5, string player6, string player7, string player8)
        {
            int MHP = GetMonsterHP(channel);
            while (MHP > 0)
            {
                Console.WriteLine("Cycling Attack");
                DoAttack(channel, player1);
                System.Threading.Thread.Sleep(3000);
                if (PlayerList[channel].Count >= 2)
                {
                    DoAttack(channel, player2);
                    System.Threading.Thread.Sleep(3000);
                }
                DoMAttack(channel);
                System.Threading.Thread.Sleep(3000);
                if (CheckAllDead(channel))
                {
                    System.Threading.Thread.Sleep(1000);
                    Console.WriteLine("All is dead, breaking");
                    break;
                }
                if (PlayerList[channel].Count >= 3)
                {
                    DoAttack(channel, player3);
                    System.Threading.Thread.Sleep(3000);
                }
                if (PlayerList[channel].Count >= 4)
                {
                    DoAttack(channel, player4);
                    System.Threading.Thread.Sleep(3000);

                }
                if (PlayerList[channel].Count >= 3)
                {
                    DoMAttack(channel);
                    System.Threading.Thread.Sleep(3000);
                    if (CheckAllDead(channel))
                    {
                        System.Threading.Thread.Sleep(1000);
                        Console.WriteLine("All is dead, breaking");
                        break;
                    }
                }
                if (PlayerList[channel].Count >= 5)
                {
                    DoAttack(channel, player5);
                    System.Threading.Thread.Sleep(3000);
                }
                if (PlayerList[channel].Count >= 6)
                {
                    DoAttack(channel, player6);
                    System.Threading.Thread.Sleep(3000);
                }
                if (PlayerList[channel].Count >= 5)
                {
                    DoMAttack(channel);
                    System.Threading.Thread.Sleep(3000);
                    if (CheckAllDead(channel))
                    {
                        System.Threading.Thread.Sleep(1000);
                        Console.WriteLine("All is dead, breaking");
                        break;
                    }
                }
                if (PlayerList[channel].Count >= 7)
                {
                    DoAttack(channel, player7);
                    System.Threading.Thread.Sleep(3000);
                }
                if (PlayerList[channel].Count >= 8)
                {
                    DoAttack(channel, player8);
                    System.Threading.Thread.Sleep(3000);
                }
                if (PlayerList[channel].Count >= 7)
                {
                    DoMAttack(channel);
                    System.Threading.Thread.Sleep(3000);
                    if (CheckAllDead(channel))
                    {
                        System.Threading.Thread.Sleep(1000);
                        Console.WriteLine("All is dead, breaking");
                        break;
                    }
                }
                MHP = GetMonsterHP(channel);
            }
            Console.WriteLine("Ending Adventure");
            EndAdventure(channel);
        }

        public string GetHelm(string channel, string user)
        {
            lock (Main._lockloot)
            {
                XElement loot = XElement.Load(@"Data\Loot.xml");
                string Helm = "Empty ";
                foreach (XElement ele in loot.Descendants(Main.GetTopic(channel, user)))
                {
                    try
                    {
                        Helm = ele.Attribute("Helm").Value.ToString();
                    }
                    catch
                    {
                        Helm = "Empty ";
                    }
                }
                return Helm;
            }
        }

        public string GetChest(string channel, string user)
        {
            lock (Main._lockloot)
            {
                XElement loot = XElement.Load(@"Data\Loot.xml");
                string Chest = "Empty ";
                foreach (XElement ele in loot.Descendants(Main.GetTopic(channel, user)))
                {
                    try
                    {
                        Chest = ele.Attribute("Chest").Value.ToString();
                    }
                    catch
                    {
                        Chest = "Empty ";
                    }
                }
                return Chest;
            }
        }

        public string GetLegs(string channel, string user)
        {
            lock (Main._lockloot)
            {
                XElement loot = XElement.Load(@"Data\Loot.xml");
                string Legs = "Empty ";
                foreach (XElement ele in loot.Descendants(Main.GetTopic(channel, user)))
                {
                    try
                    {
                        Legs = ele.Attribute("Legs").Value.ToString();
                    }
                    catch
                    {
                        Legs = "Empty ";
                    }
                }
                return Legs;
            }
        }

        public string GetBoots(string channel, string user)
        {
            lock (Main._lockloot)
            {
                XElement loot = XElement.Load(@"Data\Loot.xml");
                string Boots = "Empty ";
                foreach (XElement ele in loot.Descendants(Main.GetTopic(channel, user)))
                {
                    try
                    {
                        Boots = ele.Attribute("Boots").Value.ToString();
                    }
                    catch
                    {
                        Boots = "Empty ";
                    }
                }
                return Boots;
            }
        }

        public string GetRingLootSlot1(string channel, string user)
        {
            lock (Main._lockloot)
            {
                XElement loot = XElement.Load(@"Data\Loot.xml");
                string Slot1 = null;
                foreach (XElement ele in loot.Descendants(Main.GetTopic(channel, user)))
                {
                    XElement P = ele.Element("rings");
                    try
                    {
                        Slot1 = P.Attribute("Slot1").Value.ToString();
                    }
                    catch
                    {
                        Slot1 = null;
                    }
                }
                return Slot1;
            }
        }

        public string GetRingLootSlot2(string channel, string user)
        {
            lock (Main._lockloot)
            {
                XElement loot = XElement.Load(@"Data\Loot.xml");
                string Slot2 = null;
                foreach (XElement ele in loot.Descendants(Main.GetTopic(channel, user)))
                {
                    XElement P = ele.Element("rings");
                    try
                    {
                        Slot2 = P.Attribute("Slot2").Value.ToString();
                    }
                    catch
                    {
                        Slot2 = null;
                    }
                }
                return Slot2;
            }
        }

        public string GetRingLootSlot3(string channel, string user)
        {
            lock (Main._lockloot)
            {
                XElement loot = XElement.Load(@"Data\Loot.xml");
                string Slot3 = null;
                foreach (XElement ele in loot.Descendants(Main.GetTopic(channel, user)))
                {
                    XElement P = ele.Element("rings");
                    try
                    {
                        Slot3 = P.Attribute("Slot3").Value.ToString();
                    }
                    catch
                    {
                        Slot3 = null;
                    }
                }
                return Slot3;
            }
        }

        public string GetRingLootSlot4(string channel, string user)
        {
            lock (Main._lockloot)
            {
                XElement loot = XElement.Load(@"Data\Loot.xml");
                string Slot4 = null;
                foreach (XElement ele in loot.Descendants(Main.GetTopic(channel, user)))
                {
                    XElement P = ele.Element("rings");
                    try
                    {
                        Slot4 = P.Attribute("Slot4").Value.ToString();
                    }
                    catch
                    {
                        Slot4 = null;
                    }
                }
                return Slot4;
            }
        }

        public string GetRingLootSlot5(string channel, string user)
        {
            lock (Main._lockloot)
            {
                XElement loot = XElement.Load(@"Data\Loot.xml");
                string Slot5 = null;
                foreach (XElement ele in loot.Descendants(Main.GetTopic(channel, user)))
                {
                    XElement P = ele.Element("rings");
                    try
                    {
                        Slot5 = P.Attribute("Slot5").Value.ToString();
                    }
                    catch
                    {
                        Slot5 = null;
                    }
                }
                return Slot5;
            }
        }

        public string GetRingLootSlot6(string channel, string user)
        {
            lock (Main._lockloot)
            {
                XElement loot = XElement.Load(@"Data\Loot.xml");
                string Slot6 = null;
                foreach (XElement ele in loot.Descendants(Main.GetTopic(channel, user)))
                {
                    XElement P = ele.Element("rings");
                    try
                    {
                        Slot6 = P.Attribute("Slot6").Value.ToString();
                    }
                    catch
                    {
                        Slot6 = null;
                    }
                }
                return Slot6;
            }
        }

        public string GetLootSlot1(string channel, string user)
        {
            lock (Main._lockloot)
            {
                XElement loot = XElement.Load(@"Data\Loot.xml");
                string Slot1 = null;
                foreach (XElement ele in loot.Descendants(Main.GetTopic(channel, user)))
                {
                    try
                    {
                        Slot1 = ele.Attribute("Slot1").Value.ToString();
                    }
                    catch
                    {
                        Slot1 = null;
                    }
                }
                return Slot1;
            }
        }

        public string GetLootSlot2(string channel, string user)
        {
            lock (Main._lockloot)
            {
                XElement loot = XElement.Load(@"Data\Loot.xml");
                string Slot2 = null;
                foreach (XElement ele in loot.Descendants(Main.GetTopic(channel, user)))
                {
                    try
                    {
                        Slot2 = ele.Attribute("Slot2").Value.ToString();
                    }
                    catch
                    {
                        Slot2 = null;
                    }
                }
                return Slot2;
            }
        }

        public string GetLootSlot3(string channel, string user)
        {
            lock (Main._lockloot)
            {
                XElement loot = XElement.Load(@"Data\Loot.xml");
                string Slot3 = null;
                foreach (XElement ele in loot.Descendants(Main.GetTopic(channel, user)))
                {
                    try
                    {
                        Slot3 = ele.Attribute("Slot3").Value.ToString();
                    }
                    catch
                    {
                        Slot3 = null;
                    }
                }
                return Slot3;
            }
        }

        public string GetLootSlot4(string channel, string user)
        {
            lock (Main._lockloot)
            {
                XElement loot = XElement.Load(@"Data\Loot.xml");
                string Slot4 = null;
                foreach (XElement ele in loot.Descendants(Main.GetTopic(channel, user)))
                {
                    try
                    {
                        Slot4 = ele.Attribute("Slot4").Value.ToString();
                    }
                    catch
                    {
                        Slot4 = null;
                    }
                }
                return Slot4;
            }
        }

        public string GetLootSlot5(string channel, string user)
        {
            lock (Main._lockloot)
            {
                XElement loot = XElement.Load(@"Data\Loot.xml");
                string Slot5 = null;
                foreach (XElement ele in loot.Descendants(Main.GetTopic(channel, user)))
                {
                    try
                    {
                        Slot5 = ele.Attribute("Slot5").Value.ToString();
                    }
                    catch
                    {
                        Slot5 = null;
                    }
                }
                return Slot5;
            }
        }

        public string GetLootSlotN(string channel, string user, int slot)
        {
            lock (Main._lockloot)
            {
                XElement loot = XElement.Load(@"Data\Loot.xml");
                string Slot = null;
                foreach (XElement ele in loot.Descendants(Main.GetTopic(channel, user)))
                {
                    try
                    {
                        Slot = ele.Attribute("Slot" + slot).Value.ToString();
                    }
                    catch
                    {
                        Slot = null;
                    }
                }
                return Slot;
            }
        }

        public string GetWeapon(string channel, string user)
        {
            lock (Main._lockloot)
            {
                XElement loot = XElement.Load(@"Data\Loot.xml");
                string Weapon = null;
                foreach (XElement ele in loot.Descendants(Main.GetTopic(channel, user)))
                {
                    try
                    {
                        Weapon = ele.Attribute("Sword").Value.ToString();
                    }
                    catch
                    {
                        Weapon = null;
                    }
                }
                if (Weapon == null)
                {
                    return "Bare Hands";
                }
                else
                {
                    return Weapon;
                }
            }
        }

        public string GetRing(string channel, string user)
        {
            lock (Main._lockloot)
            {
                XElement loot = XElement.Load(@"Data\Loot.xml");
                string Ring = null;
                foreach (XElement ele in loot.Descendants(Main.GetTopic(channel, user)))
                {
                    try
                    {
                        Ring = ele.Attribute("Ring").Value.ToString();
                    }
                    catch
                    {
                        Ring = null;
                    }
                }
                if (Ring == null)
                {
                    return "Empty ";
                }
                else
                {
                    return Ring;
                }
            }
        }

        public bool IsCrit(string channel, string user)
        {
            double critChance = ((((GetPlayerInt(channel, user) + GetPlayerDex(channel, user)) / 2) / GetMonsterDex(channel)) * 5);
            critChance = Convert.ToDouble(critChance.ToString("#.000"));
            if (GetWeapon(channel, user).Contains("Axe"))
            {
                critChance *= 1.1;
            }
            if (GetRing(channel, user).Contains("Precision"))
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

        public bool IsMCrit(string channel, string target)
        {
            double critChance = ((((GetMonsterInt(channel) + GetMonsterDex(channel)) / 2) / GetPlayerDex(channel, target)) * 5);
            critChance = Convert.ToDouble(critChance.ToString("#.000"));
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
            double dodgeChance = (((GetPlayerDex(channel, user) * 1.3) / (GetMonsterInt(channel) + GetMonsterDex(channel) * 0.8)) * 4);
            dodgeChance = Convert.ToDouble(dodgeChance.ToString("#.000"));
            if (GetWeapon(channel, user).Contains("Bow"))
            {
                dodgeChance *= 1.2;
            }
            if (GetRing(channel, user).Contains("Avoidance"))
            {
                dodgeChance *= 1.2;
            }
            return dodgeChance;
        }

        public double GenMDodgeChance(string channel, string target)
        {
            double dodgeChance = (((GetMonsterDex(channel) * 1.3) / (GetPlayerInt(channel, target) + GetPlayerDex(channel, target) * 0.8)) * 4);
            dodgeChance = Convert.ToDouble(dodgeChance.ToString("#.000"));
            return dodgeChance;
        }

        private double GetAttackMultiplier(string channel, string user)
        {
            XDocument Data = XDocument.Load(@"Data\LootData.xml");
            XElement getData = Data.Element("loot").Element("stats").Element("sword");
            string Weapon = GetWeapon(channel, user);
            int position = Weapon.IndexOf(" ");
            string type = Weapon.Substring(0, position).ToUpper();
            double Multiply = Convert.ToDouble(getData.Attribute(type).Value);
            double[] unknown = { 0.01, 0.2, 0.5, 0.7, 0.9, 1, 1.4, 1.7, 1.8, 1.4, 9.0 };
            if (GetWeapon(channel, user).Contains("Axe"))
            {
                Multiply = Multiply * 1.2;
            }
            else if (GetWeapon(channel, user).Contains("Hammer"))
            {
                Multiply = Multiply * 0.8;
            }
            else if (GetWeapon(channel, user).Contains("Unknown"))
            {
                Multiply = unknown[random.Next(unknown.Length)];
            }
            List<string> Equipped = new List<string>
            {
                GetHelm(channel, user),
                GetChest(channel, user),
                GetLegs(channel, user),
                GetBoots(channel, user),
                GetWeapon(channel, user)
            };
            double TotalMultiply;
            if ((Equipped[0]).ToUpper().Contains("ERIDIN") && (Equipped[1]).ToUpper().Contains("ERIDIN") && (Equipped[2]).ToUpper().Contains("ERIDIN") && (Equipped[3]).ToUpper().Contains("ERIDIN") && (Equipped[4]).ToUpper().Contains("ERIDIN"))
            {
                TotalMultiply = Multiply * 1.05;
            }
            else if ((Equipped[0]).ToUpper().Contains("BRAHMA") && (Equipped[1]).ToUpper().Contains("BRAHMA") && (Equipped[2]).ToUpper().Contains("BRAHMA") && (Equipped[3]).ToUpper().Contains("BRAHMA") && (Equipped[4]).ToUpper().Contains("BRAHMA"))
            {
                TotalMultiply = Multiply * 1.075;
            }
            else if ((Equipped[0]).ToUpper().Contains("NIGHT") && (Equipped[1]).ToUpper().Contains("NIGHT") && (Equipped[2]).ToUpper().Contains("NIGHT") && (Equipped[3]).ToUpper().Contains("NIGHT") && (Equipped[4]).ToUpper().Contains("NIGHT"))
            {
                TotalMultiply = Multiply * 1.1;
            }
            else if ((Equipped[0]).ToUpper().Contains("DESTINY") && (Equipped[1]).ToUpper().Contains("DESTINY") && (Equipped[2]).ToUpper().Contains("DESTINY") && (Equipped[3]).ToUpper().Contains("DESTINY") && (Equipped[4]).ToUpper().Contains("DESTINY"))
            {
                TotalMultiply = Multiply * 1.15;
            }
            else if ((Equipped[0]).ToUpper().Contains("ENLIGHTENMENT") && (Equipped[1]).ToUpper().Contains("ENLIGHTENMENT") && (Equipped[2]).ToUpper().Contains("ENLIGHTENMENT") && (Equipped[3]).ToUpper().Contains("ENLIGHTENMENT") && (Equipped[4]).ToUpper().Contains("ENLIGHTENMENT"))
            {
                TotalMultiply = Multiply * 1.08;
            }
            else if ((Equipped[0]).ToUpper().Contains("MYSTERIES") && (Equipped[1]).ToUpper().Contains("MYSTERIES") && (Equipped[2]).ToUpper().Contains("MYSTERIES") && (Equipped[3]).ToUpper().Contains("MYSTERIES") && (Equipped[4]).ToUpper().Contains("MYSTERIES"))
            {
                TotalMultiply = Multiply * 1.12;
            }
            else if ((Equipped[0]).ToUpper().Contains("LOST") && (Equipped[1]).ToUpper().Contains("LOST") && (Equipped[2]).ToUpper().Contains("LOST") && (Equipped[3]).ToUpper().Contains("LOST") && (Equipped[4]).ToUpper().Contains("LOST"))
            {
                TotalMultiply = Multiply * 1.14;
            }
            else
            {
                TotalMultiply = Multiply;
            }
            if (GetRing(channel, user).Contains("Might"))
            {
                TotalMultiply *= 1.2;
            }
            return TotalMultiply;
        }

        public double GetDefenseMultiplier(string channel, string user)
        {
            XDocument Data = XDocument.Load(@"Data\LootData.xml");
            XElement getData = Data.Element("loot").Element("stats").Element("armour");
            string Helm = GetHelm(channel, user);
            int positionHelm = Helm.IndexOf(" ");
            string typeHelm = Helm.Substring(0, positionHelm).ToUpper();
            double MultiplyHelm = Convert.ToDouble(getData.Attribute(typeHelm).Value.ToString());
            string Chest = GetChest(channel, user);
            int positionChest = Chest.IndexOf(" ");
            string typeChest = Chest.Substring(0, positionChest).ToUpper();
            double MultiplyChest = Convert.ToDouble(getData.Attribute(typeChest).Value.ToString());
            string Legs = GetChest(channel, user);
            int positionLegs = Legs.IndexOf(" ");
            string typeLegs = Legs.Substring(0, positionLegs).ToUpper();
            double MultiplyLegs = Convert.ToDouble(getData.Attribute(typeLegs).Value.ToString());
            string Boots = GetBoots(channel, user);
            int positionBoots = Boots.IndexOf(" ");
            string typeBoots = Boots.Substring(0, positionBoots).ToUpper();
            double MultiplyBoots = Convert.ToDouble(getData.Attribute(typeBoots).Value.ToString());
            double Multiply = MultiplyHelm + MultiplyChest + MultiplyLegs + MultiplyBoots + 1;
            if (GetWeapon(channel, user).Contains("Axe"))
            {
                Multiply = Multiply * 0.75;
            }
            else if (GetWeapon(channel, user).Contains("Hammer"))
            {
                Multiply = Multiply * 1.2;
            }
            List<string> Equipped = new List<string>
            {
                GetHelm(channel, user),
                GetChest(channel, user),
                GetLegs(channel, user),
                GetBoots(channel, user),
                GetWeapon(channel, user)
            };
            double TotalMultiply;
            if ((Equipped[0]).ToUpper().Contains("ERIDIN") && (Equipped[1]).ToUpper().Contains("ERIDIN") && (Equipped[2]).ToUpper().Contains("ERIDIN") && (Equipped[3]).ToUpper().Contains("ERIDIN") && (Equipped[4]).ToUpper().Contains("ERIDIN"))
            {
                TotalMultiply = Multiply * 1.05;
            }
            else if ((Equipped[0]).ToUpper().Contains("BRAHMA") && (Equipped[1]).ToUpper().Contains("BRAHMA") && (Equipped[2]).ToUpper().Contains("BRAHMA") && (Equipped[3]).ToUpper().Contains("BRAHMA") && (Equipped[4]).ToUpper().Contains("BRAHMA"))
            {
                TotalMultiply = Multiply * 1.075;
            }
            else if ((Equipped[0]).ToUpper().Contains("NIGHT") && (Equipped[1]).ToUpper().Contains("NIGHT") && (Equipped[2]).ToUpper().Contains("NIGHT") && (Equipped[3]).ToUpper().Contains("NIGHT") && (Equipped[4]).ToUpper().Contains("NIGHT"))
            {
                TotalMultiply = Multiply * 1.1;
            }
            else if ((Equipped[0]).ToUpper().Contains("DESTINY") && (Equipped[1]).ToUpper().Contains("DESTINY") && (Equipped[2]).ToUpper().Contains("DESTINY") && (Equipped[3]).ToUpper().Contains("DESTINY") && (Equipped[4]).ToUpper().Contains("DESTINY"))
            {
                TotalMultiply = Multiply * 1.15;
            }
            else if ((Equipped[0]).ToUpper().Contains("ENLIGHTENMENT") && (Equipped[1]).ToUpper().Contains("ENLIGHTENMENT") && (Equipped[2]).ToUpper().Contains("ENLIGHTENMENT") && (Equipped[3]).ToUpper().Contains("ENLIGHTENMENT") && (Equipped[4]).ToUpper().Contains("ENLIGHTENMENT"))
            {
                TotalMultiply = Multiply * 1.08;
            }
            else if ((Equipped[0]).ToUpper().Contains("MYSTERIES") && (Equipped[1]).ToUpper().Contains("MYSTERIES") && (Equipped[2]).ToUpper().Contains("MYSTERIES") && (Equipped[3]).ToUpper().Contains("MYSTERIES") && (Equipped[4]).ToUpper().Contains("MYSTERIES"))
            {
                TotalMultiply = Multiply * 1.12;
            }
            else if ((Equipped[0]).ToUpper().Contains("LOST") && (Equipped[1]).ToUpper().Contains("LOST") && (Equipped[2]).ToUpper().Contains("LOST") && (Equipped[3]).ToUpper().Contains("LOST") && (Equipped[4]).ToUpper().Contains("LOST"))
            {
                TotalMultiply = Multiply * 1.14;
            }
            else
            {
                TotalMultiply = Multiply;
            }
            if (GetRing(channel, user).Contains("Protection"))
            {
                TotalMultiply *= 1.2;
            }
            return TotalMultiply;
        }

        public int GetAttackDmg(string channel, string user)
        {
            double[] token = { 2, 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8, 2.9, 3 };
            int Dmg;
            if (GetWeapon(channel, user).Contains("Bow"))
            {
                Dmg = Convert.ToInt32(Math.Floor((((GetPlayerArc(channel, user) + GetPlayerEnd(channel, user)) * token[random.Next(token.Length)]) * GetAttackMultiplier(channel, user))));
            }
            else
            {
                Dmg = Convert.ToInt32(Math.Floor((((GetPlayerStr(channel, user) + GetPlayerEnd(channel, user)) * token[random.Next(token.Length)]) * GetAttackMultiplier(channel, user))));
            }
            return Dmg;
        }

        private void WriteMHP(string channel, int AttackDmg)
        {
            lock (Main._lockadv)
            {
                XDocument Adv = XDocument.Load(@"Data\Adventure.xml");
                XElement M = Adv.Element("adventure").Element("Channel." + channel).Element(channel + ".Monster");
                int MHP = Convert.ToInt32(M.Attribute("HP").Value);
                M.SetAttributeValue("HP", MHP - AttackDmg);
                Adv.Save(@"Data\Adventure.xml");
            }
        }

        private void WritePlayerHP(string channel, string user, int AttackDmg)
        {
            lock (Main._lockadv)
            {
                XDocument Adv = XDocument.Load(@"Data\Adventure.xml");
                XElement P = Adv.Element("adventure").Element("Channel." + channel).Element(channel + "." + user);
                int PHP = Convert.ToInt32(P.Attribute("HP").Value);
                P.SetAttributeValue("HP", PHP - AttackDmg);
                Adv.Save(@"Data\Adventure.xml");
            }
        }

        private void TryHeal(string channel, string user)
        {
            if (random.Next(1, 6) == 4)
            {
                lock (Main._lockadv)
                {
                    XDocument Adv = XDocument.Load(@"Data\Adventure.xml");
                    XElement P = Adv.Element("adventure").Element("Channel." + channel).Element(channel + "." + user);
                    int PHP = Convert.ToInt32(P.Attribute("HP").Value);
                    double Heals;
                    if (PHP + (Math.Floor(Convert.ToDouble(GetPlayerHP(channel, user)) / 5)) > GetPlayerHP(channel, user))
                    {
                        Heals = GetPlayerHP(channel, user);
                    }
                    else
                    {
                        Heals = (PHP + Math.Floor(Convert.ToDouble(GetPlayerHP(channel, user)) / 5));
                    }
                    P.SetAttributeValue("HP", (Heals));
                    Adv.Save(@"Data\Adventure.xml");
                    foreach (string plyr in PlayerList[channel])
                    {
                        Main.client.SendWhisper(plyr, "[HEAL] " + user.ToLower() + "'s " + GetRing(channel, user) + " has healed them, they are now at (" + Heals + ") HP.");
                    }
                    Console.WriteLine("Healed!");
                }
            }
        }

        private void WriteAttackDmg(string channel, string user, int AttackDmg)
        {
            lock (Main._lockadv)
            {
                XDocument Adv = XDocument.Load(@"Data\Adventure.xml");
                XElement P = Adv.Element("adventure").Element("Channel." + channel).Element(channel + "." + user);
                int CurDmg = Convert.ToInt32(P.Attribute("TotalDmg").Value);
                P.SetAttributeValue("Dmg", CurDmg + AttackDmg);
                P.SetAttributeValue("TotalDmg", CurDmg + AttackDmg);
                Adv.Save(@"Data\Adventure.xml");
            }
        }

        public int GenMAttack(string channel, string user)
        {
            double[] token = { 1, 1.1, 1.2, 1.3, 1.4, 1.5 };
            int Atk = Convert.ToInt32(Math.Floor(((GetMonsterStr(channel) + (GetMonsterEnd(channel) / 1.7) * token[random.Next(token.Length)])) / GetDefenseMultiplier(channel, user)));
            return Atk;
        }

        public int GenM150Attack(string channel, string user)
        {
            double[] token = { 1, 1.05, 1.1, 1.15, 1.2, 1.25, 1.3, 1.35, 1.4, 1.45, 1.5 };
            int Atk = Convert.ToInt32(Math.Floor(((GetMonsterStr(channel) + (GetMonsterEnd(channel) / 1.7) * token[random.Next(token.Length)])) / GetDefenseMultiplier(channel, user)));
            return Atk;
        }

        private int GenMRaidAttack(string channel, string user)
        {
            double[] token = { 1.9, 2, 2.1, 2.2, 2.3, 2.4 };
            int Atk = Convert.ToInt32(Math.Floor((((GetMonsterStr(channel) * 2) + (GetMonsterEnd(channel) * 2.5) * token[random.Next(token.Length)])) / GetDefenseMultiplier(channel, user)));
            return Atk;
        }

        private void DoAttack(string channel, string player)
        {
            if (CheckDead(channel, player))
            {
                foreach (string plyr in PlayerList[channel])
                {
                    Main.client.SendWhisper(plyr, player.ToLower() + " is dead and can't attack!");
                }
            }
            else
            {
                string weapon = GetWeapon(channel, player);
                int AttackDmg = GetAttackDmg(channel, player);
                double MissChance; XElement adv;
                lock (Main._lockadv)
                {
                    adv = XElement.Load(@"Data\Adventure.xml");
                }
                string monster = null;
                foreach (XElement ele in adv.Descendants(channel + ".Monster"))
                {
                    monster = ele.Attribute("Monster").Value.ToString();
                }
                if (GetWeapon(channel, player).Contains("Bow"))
                {
                    MissChance = Math.Sqrt(GetPlayerArc(channel, player) + 20) * 0.8;
                }
                else
                {
                    MissChance = 100000.0;
                }
                if (MissChance * 1000 >= random.Next(1, 10001))
                {
                    if (Is150(channel))
                    {
                        if ((GenMDodgeChance(channel, player) * 1000) >= random.Next(1, 100000))
                        {
                            foreach (string plyr in PlayerList[channel])
                            {
                                Main.client.SendWhisper(plyr, "[DODGE] " + monster + " has dodged the attack!");
                            }
                        }
                        else if (IsCrit(channel, player))
                        {
                            AttackDmg *= 2;
                            WriteMHP(channel, AttackDmg);
                            foreach (string plyr in PlayerList[channel])
                            {
                                Main.client.SendWhisper(plyr, "[CRITICAL] " + player.ToLower() + " has done (" + AttackDmg + ") dmg with their " + weapon + ". (" + GetMonsterHP(channel) + "HP Remaining)");
                            }
                            WriteAttackDmg(channel, player, AttackDmg);
                        }
                        else
                        {
                            WriteMHP(channel, AttackDmg);
                            foreach (string plyr in PlayerList[channel])
                            {
                                Main.client.SendWhisper(plyr, player.ToLower() + " has done (" + AttackDmg + ") dmg with their " + weapon + ". (" + GetMonsterHP(channel) + "HP Remaining)");
                            }
                            WriteAttackDmg(channel, player, AttackDmg);
                        }
                    }
                    else
                    {
                        if (IsCrit(channel, player))
                        {
                            AttackDmg *= 2;
                            WriteMHP(channel, AttackDmg);
                            foreach (string plyr in PlayerList[channel])
                            {
                                Main.client.SendWhisper(plyr, "[CRITICAL] " + player.ToLower() + " has done (" + AttackDmg + ") dmg with their " + weapon + ". (" + GetMonsterHP(channel) + "HP Remaining)");
                            }
                            WriteAttackDmg(channel, player, AttackDmg);
                        }
                        else
                        {
                            WriteMHP(channel, AttackDmg);
                            foreach (string plyr in PlayerList[channel])
                            {
                                Main.client.SendWhisper(plyr, player.ToLower() + " has done (" + AttackDmg + ") dmg with their " + weapon + ". (" + GetMonsterHP(channel) + "HP Remaining)");
                            }
                            WriteAttackDmg(channel, player, AttackDmg);
                        }
                    }
                }
                else
                {
                    foreach (string plyr in PlayerList[channel])
                    {
                        Main.client.SendWhisper(plyr, "[MISS] " + player.ToLower() + " has missed their attack using their " + weapon + ".");
                    }
                }
                if (GetRing(channel, player).Contains("Recovery"))
                {
                    Console.WriteLine("Trying Heals");
                    TryHeal(channel, player);
                }
            }
        }

        private string GetHighestThreat(string channel)
        {
            XDocument Adv = XDocument.Load(@"Data\Adventure.xml");
            int HighestDmg = 0;
            string HighestPlayer = PlayerList[channel][0];
            foreach (string plyr in PlayerList[channel])
            {
                XElement P = Adv.Element("adventure").Element("Channel." + channel).Element(channel + "." + plyr);
                if (!CheckDead(channel, plyr))
                {
                    int Dmg = Convert.ToInt32(P.Attribute("TotalDmg").Value);
                    if (Dmg > HighestDmg)
                    {
                        HighestDmg = Dmg;
                        HighestPlayer = plyr;
                    }
                }
            }
            return HighestPlayer;
        }

        private void DoMAttack(string channel)
        {
            bool IsDead = true;
            string target = null;
            string monster = null;
            XElement adv;
            lock (Main._lockadv)
            {
                adv = XElement.Load(@"Data\Adventure.xml");
            }
            foreach (XElement ele in adv.Descendants(channel + ".Monster"))
            {
                monster = ele.Attribute("Monster").Value.ToString();
            }
            while (IsDead)
            {
                int index = random.Next(PlayerList[channel].Count);
                target = PlayerList[channel][index];
                if (!CheckDead(channel, target))
                {
                    break;
                }
            }
            int AttackDmg = GenMAttack(channel, target);
            if (IsRaid(channel))
            {
                target = GetHighestThreat(channel);
                AttackDmg = GenMRaidAttack(channel, target);
            }
            if (Is150(channel))
            {
                target = GetHighestThreat(channel);
                AttackDmg = GenM150Attack(channel, target);
            }
            if (GetMonsterHP(channel) > 0)
            {
                if ((GenDodgeChance(channel, target) * 1000) >= random.Next(1, 100000))
                {
                    foreach (string plyr in PlayerList[channel])
                    {
                        Main.client.SendWhisper(plyr, "[DODGE] " + target.ToLower() + " has dodged the attack!");
                    }
                }
                else
                {
                    if (Is150(channel))
                    {
                        if (IsMCrit(channel, target))
                        {
                            AttackDmg *= 2;
                            WritePlayerHP(channel, target, AttackDmg);
                            foreach (string plyr in PlayerList[channel])
                            {
                                Main.client.SendWhisper(plyr, "[CRITICAL] " + monster + " has done (" + AttackDmg + ") dmg to " + target.ToLower() + ". (" + GetAdvPlayerHP(channel, target) + "HP Remaining)");
                            }
                        }
                        else
                        {
                            WritePlayerHP(channel, target, AttackDmg);
                            foreach (string plyr in PlayerList[channel])
                            {
                                Main.client.SendWhisper(plyr, monster + " has done (" + AttackDmg + ") dmg to " + target.ToLower() + ". (" + GetAdvPlayerHP(channel, target) + "HP Remaining)");
                            }
                        }
                    }
                    else
                    {
                        WritePlayerHP(channel, target, AttackDmg);
                        foreach (string plyr in PlayerList[channel])
                        {
                            Main.client.SendWhisper(plyr, monster + " has done (" + AttackDmg + ") dmg to " + target.ToLower() + ". (" + GetAdvPlayerHP(channel, target) + "HP Remaining)");
                        }
                    }
                    System.Threading.Thread.Sleep(100);
                    if (GetAdvPlayerHP(channel, target) <= 0)
                    {
                        System.Threading.Thread.Sleep(100);
                        Main.client.SendMessage(channel, "/me : " + target.ToLower() + " is now dead!");
                        foreach (string plyr in PlayerList[channel])
                        {
                            System.Threading.Thread.Sleep(100);
                            Main.client.SendWhisper(plyr, target.ToLower() + " is now dead!");
                        }
                        System.Threading.Thread.Sleep(100);
                        lock (Main._lockadv)
                        {
                            XDocument Adv2 = XDocument.Load(@"Data\Adventure.xml");
                            XElement Trgt2 = Adv2.Element("adventure").Element("Channel." + channel).Element(channel + "." + target);
                            Trgt2.SetAttributeValue("Dead", "true");
                            Adv2.Save(@"Data\Adventure.xml");
                        }
                    }
                }
            }
            else
            {
            }
        }

        private void GiveXP(string channel, string user)
        {
            lock (Main._lockxp)
            {
                XDocument XP = XDocument.Load(@"Data\XP.xml");
                XElement P = XP.Element("users").Element(Main.GetTopic(channel, user));
                XDocument Adv = XDocument.Load(@"Data\Adventure.xml");
                XElement PA = Adv.Element("adventure").Element("Channel." + channel).Element(channel + "." + user);
                int XPNow = Convert.ToInt32(P.Attribute("XP").Value.ToString());
                double Dmg = Convert.ToDouble(PA.Attribute("TotalDmg").Value.ToString());
                int XPToGive;
                if (IsRaid(channel))
                {
                    XPToGive = (GetMonsterLvl(channel) * 30) + 250;
                }
                else if (CheckDead(channel, user))
                {
                    if (Is150(channel))
                    {
                        XPToGive = Convert.ToInt32(Math.Floor(Dmg / 2 / 7));
                    }
                    else
                    {
                        XPToGive = Convert.ToInt32(Math.Floor(Dmg / 2 / 10));
                    }
                }
                else
                {
                    if (Is150(channel))
                    {
                        XPToGive = Convert.ToInt32(Math.Floor(Dmg / 7));
                    }
                    else
                    {
                        XPToGive = Convert.ToInt32(Math.Floor(Dmg / 10));
                    }
                }
                if (GetRing(channel, user).Contains("Training"))
                {
                    XPToGive = Convert.ToInt32(Math.Floor(XPToGive * 1.5));
                }
                System.Threading.Thread.Sleep(100);
                Main.client.SendWhisper(user.ToLower(), "You earnt (" + XPToGive + ") XP");
                int XPWrite = XPToGive + XPNow;
                P.SetAttributeValue("XP", XPWrite);
                XP.Save(@"Data\XP.xml");
            }
        }

        public string GetNextLootSlot(string channel, string user)
        {
            CheckLootUser(channel, user);
            string Slot = "error";
            Shop shop = new Shop();
            int s = 1;
            while (s <= shop.GetMaxBagSlots(channel, user))
            {
                if (GetLootSlotN(channel, user, s) == null)
                {
                    Slot = "Slot" + s;
                    break;
                }
                s++;
            }
            //if (GetLootSlot1(channel, user) == null)
            //{
            //    Slot = "Slot1";
            //}
            //else if (GetLootSlot2(channel, user) == null)
            //{
            //    Slot = "Slot2";
            //}
            //else if (GetLootSlot3(channel, user) == null)
            //{
            //    Slot = "Slot3";
            //}
            //else if (GetLootSlot4(channel, user) == null)
            //{
            //    Slot = "Slot4";
            //}
            //else if (GetLootSlot5(channel, user) == null)
            //{
            //    Slot = "Slot5";
            //}
            //else
            //{
            //    Slot = "Error";
            //}
            return Slot;
        }

        public string GetNextRingLootSlot(string channel, string user)
        {
            CheckLootUser(channel, user);
            string Slot;
            if (GetRingLootSlot1(channel, user) == null)
            {
                Slot = "Slot1";
            }
            else if (GetRingLootSlot2(channel, user) == null)
            {
                Slot = "Slot2";
            }
            else if (GetRingLootSlot3(channel, user) == null)
            {
                Slot = "Slot3";
            }
            else if (GetRingLootSlot4(channel, user) == null)
            {
                Slot = "Slot4";
            }
            else if (GetRingLootSlot5(channel, user) == null)
            {
                Slot = "Slot5";
            }
            else if (GetRingLootSlot6(channel, user) == null)
            {
                Slot = "Slot6";
            }
            else
            {
                Slot = "Error";
            }
            return Slot;
        }

        private void GiveLoot(string channel, string user)
        {
            Shop shop = new Shop();
            string Loot = MakeLoot(channel, user);
            if (IsHard(channel))
            {
                Loot = MakeHardLoot(channel, user);
            }
            Main.client.SendMessage(channel, "/me : [LOOT] " + user.ToLower() + " found loot: " + Loot);
            CheckLootUser(channel, user);
            lock (Main._lockloot)
            {
                XDocument loot = XDocument.Load(@"Data\Loot.xml");
                XElement P = loot.Element("loot").Element(Main.GetTopic(channel, user));
                if (!shop.IsBagFull(channel, user))
                {
                    P.SetAttributeValue(GetNextLootSlot(channel, user), Loot);
                }
                else
                {
                    Main.client.SendMessage(channel, "/me : [LOOT] " + user.ToLower() + " couldnt carry their loot home and had to leave it behind :(");
                }
                loot.Save(@"Data\Loot.xml");
            }
            if (shop.IsBagFull(channel, user))
            {
                Main.client.SendWhisper(user, "Your bag is now full, make sure to clear some space before the next adventure.");
            }
        }

        public bool IsRingBagFull(string channel, string user)
        {
            Adventure adventure = new Adventure();
            int i = 1;
            bool BagFull = true;
            Dictionary<int, string> methods = new Dictionary<int, string>
            {
                { 1, adventure.GetRingLootSlot1(channel, user) },
                { 2, adventure.GetRingLootSlot2(channel, user) },
                { 3, adventure.GetRingLootSlot3(channel, user) },
                { 4, adventure.GetRingLootSlot4(channel, user) },
                { 5, adventure.GetRingLootSlot5(channel, user) },
                { 6, adventure.GetRingLootSlot6(channel, user) }
            };
            while (i <= 6)
            {
                if (methods[i] == null)
                {
                    BagFull = false;
                    break;
                }
                i++;
            }
            return BagFull;
        }

        private void GiveRaidLoot(string channel, string user)
        {
            Shop shop = new Shop();
            if ((GetPlayerLoo(channel, user) / 100 + 1) * 1000 >= random.Next(1, 3001))
            {
                int LootChance = ((GetMonsterLvl(channel) + 5) / 3);
                string Loot;
                if (random.Next(1, 6) < 5)
                {
                    Loot = MakeRaidLoot(channel, user, LootChance);
                }
                else
                {
                    Loot = MakeRing(LootChance);
                }
                Main.client.SendMessage(channel, "/me : [LOOT] " + user.ToLower() + " found loot: " + Loot);
                CheckLootUser(channel, user);
                lock (Main._lockloot)
                {
                    XDocument loot = XDocument.Load(@"Data\Loot.xml");
                    XElement P = loot.Element("loot").Element(Main.GetTopic(channel, user));
                    if (Loot.Contains("Ring"))
                    {
                        P = P.Element("rings");
                        if (!IsRingBagFull(channel, user))
                        {
                            P.SetAttributeValue(GetNextRingLootSlot(channel, user), Loot);
                        }
                        else
                        {
                            Main.client.SendMessage(channel, "/me : [LOOT] " + user.ToLower() + " couldnt carry their loot home and had to leave it behind :(");
                        }
                    }
                    else
                    {
                        if (!shop.IsBagFull(channel, user))
                        {
                            P.SetAttributeValue(GetNextLootSlot(channel, user), Loot);
                        }
                        else
                        {
                            Main.client.SendMessage(channel, "/me : [LOOT] " + user.ToLower() + " couldnt carry their loot home and had to leave it behind :(");
                        }
                    }
                    loot.Save(@"Data\Loot.xml");
                }
            }
            if (shop.IsBagFull(channel, user))
            {
                Main.client.SendWhisper(user, "Your bag is now full, make sure to clear some space before the next adventure.");
            }
        }

        private void Give150Loot(string channel, string user)
        {
            Shop shop = new Shop();
            if ((GetPlayerLoo(channel, user) / 100 + 1) * 1000 >= random.Next(1, 4001))
            {
                int LootChance = ((GetMonsterLvl(channel) + 20) / 2);
                string Loot;
                if (random.Next(1, 6) < 5)
                {
                    Loot = Make150Loot(channel, user, LootChance);
                }
                else
                {
                    Loot = MakeRing(LootChance);
                }
                Main.client.SendMessage(channel, "/me : [LOOT] " + user.ToLower() + " found loot: " + Loot);
                CheckLootUser(channel, user);
                lock (Main._lockloot)
                {
                    XDocument loot = XDocument.Load(@"Data\Loot.xml");
                    XElement P = loot.Element("loot").Element(Main.GetTopic(channel, user));
                    if (Loot.Contains("Ring"))
                    {
                        P = P.Element("rings");
                        if (!IsRingBagFull(channel, user))
                        {
                            P.SetAttributeValue(GetNextRingLootSlot(channel, user), Loot);
                        }
                        else
                        {
                            Main.client.SendMessage(channel, "/me : [LOOT] " + user.ToLower() + " couldnt carry their loot home and had to leave it behind :(");
                        }
                    }
                    else
                    {
                        if (!shop.IsBagFull(channel, user))
                        {
                            P.SetAttributeValue(GetNextLootSlot(channel, user), Loot);
                        }
                        else
                        {
                            Main.client.SendMessage(channel, "/me : [LOOT] " + user.ToLower() + " couldnt carry their loot home and had to leave it behind :(");
                        }
                    }
                    loot.Save(@"Data\Loot.xml");
                }
            }
            if (shop.IsBagFull(channel, user))
            {
                Main.client.SendWhisper(user, "Your bag is now full, make sure to clear some space before the next adventure.");
            }
        }

        public string LootTest()
        {
            int LootChance = ((18 + 5) / 3);
            string Loot;
            if (random.Next(6) < 5)
            {
                Loot = MakeRaidLoot("T3MPU5_FU91T_", "T3MPU5_FU91T_", LootChance);
            }
            else
            {
                Loot = MakeRing(LootChance);
            }
            return Loot;
        }

        private void GiveCoin(string channel, string user)
        {
            Shop shop = new Shop();
            int Coins = random.Next(1, (GetMonsterLvl(channel) + 1));
            Main.client.SendMessage(channel, "/me : [LOOT] " + user.ToLower() + " found loot: " + Coins + " Gold Coins");
            CheckLootUser(channel, user);
            int s = 1;
            string Slot = null;
            while (s <= shop.GetMaxBagSlots(channel, user))
            {
                string Loot = GetLootSlotN(channel, user, s);
                if (Loot != null && Loot.Contains("Coins"))
                {
                    Slot = "Slot" + s;
                    break;
                }
                s++;
            }
            lock (Main._lockloot)
            {
                XDocument loot = XDocument.Load(@"Data\Loot.xml");
                XElement P = loot.Element("loot").Element(Main.GetTopic(channel, user));
                if (Slot != null)
                {
                    P.SetAttributeValue(Slot, (Coins + Convert.ToInt32(GetLootSlotN(channel, user, s).Substring(0, GetLootSlotN(channel, user, s).IndexOf(" ")))) + " Gold Coins");
                }
                else if (!shop.IsBagFull(channel, user))
                {

                    P.SetAttributeValue(GetNextLootSlot(channel, user), Coins + " Gold Coins");
                }
                else
                {
                    Main.client.SendMessage(channel, "/me : [LOOT] " + user.ToLower() + " couldnt carry their loot home and had to leave it behind :(");
                }
                loot.Save(@"Data\Loot.xml");
            }
        }

        public string MakeLoot(string channel, string user)
        {
            int Chance1 = random.Next(101);
            int Chance2 = random.Next(101);
            string LootPrefix;
            string LootSuffix;
            if (GetRing(channel, user).Contains("Spoils"))
            {
                Chance1 = random.Next(50, 101);
                Chance2 = random.Next(50, 101);
            }
            if (Chance1 <= 60)
            {
                LootPrefix = "Wooden ";
            }
            else if (Chance1 <= 92)
            {
                LootPrefix = "Steel ";
            }
            else if (Chance1 <= 99)
            {
                LootPrefix = "Moonstone ";
            }
            else
            {
                LootPrefix = "Fire ";
            }
            string[] Type = { "Sword", "Axe", "Bow", "Hammer", "Helm", "Chest", "Legs", "Boots" };
            string LootType = Type[random.Next(Type.Length)];
            if (Chance2 <= 60)
            {
                LootSuffix = null;
            }
            else if (Chance2 <= 80)
            {
                LootSuffix = " of Eridin";
            }
            else if (Chance2 <= 90)
            {
                LootSuffix = " of Brahma";
            }
            else if (Chance2 <= 99)
            {
                LootSuffix = " of Night and Day";
            }
            else
            {
                LootSuffix = " of Destiny 2.0";
            }
            string Loot = LootPrefix + LootType + LootSuffix;
            return Loot;
        }

        public string MakeRing(int chance)
        {
            int Chance1 = random.Next(1, 101);
            string Loot;
            if (chance >= random.Next(1, 101))
            {
                if (Chance1 <= 75)
                {
                    Loot = "Ring of Precision";
                }
                else if (Chance1 <= 88)
                {
                    Loot = "Ring of Spoils";
                }
                else
                {
                    Loot = "Ring of Recovery";
                }
            }
            else
            {
                if (Chance1 <= 20)
                {
                    Loot = "Ring of Might";
                }
                else if (Chance1 <= 45)
                {
                    Loot = "Ring of Avoidance";
                }
                else if (Chance1 <= 60)
                {
                    Loot = "Ring of Training";
                }
                else if (Chance1 <= 88)
                {
                    Loot = "Ring of Protection";
                }
                else
                {
                    Loot = "Ring of Precision";
                }
            }
            return Loot;
        }

        public string MakeRaidLoot(string channel, string user, int chance)
        {
            int Chance1 = random.Next(1, 101);
            int Chance2 = random.Next(1, 101);
            string LootPrefix;
            string LootSuffix;
            string Loot;
            if (GetRing(channel, user).Contains("Spoils"))
            {
                Chance1 = random.Next(50, 101);
                Chance2 = random.Next(50, 101);
            }
            if (chance >= random.Next(1, 101))
            {
                if (Chance1 <= 75)
                {
                    LootPrefix = "Fire ";
                }
                else
                {
                    LootPrefix = "Enchanted ";
                }
                string[] Type = { "Sword", "Axe", "Bow", "Hammer", "Helm", "Chest", "Legs", "Boots" };
                string LootType = Type[random.Next(Type.Length)];
                if (Chance2 <= 50)
                {
                    LootSuffix = " of Eridin";
                }
                else if (Chance2 <= 75)
                {
                    LootSuffix = " of Brahma";
                }
                else if (Chance2 <= 90)
                {
                    LootSuffix = " of Night and Day";
                }
                else
                {
                    LootSuffix = " of Destiny 2.0";
                }
                Loot = LootPrefix + LootType + LootSuffix;
            }
            else
            {
                if (Chance1 <= 60)
                {
                    LootPrefix = "Steel ";
                }
                else if (Chance1 <= 95)
                {
                    LootPrefix = "Moonstone ";
                }
                else if (Chance1 <= 99)
                {
                    LootPrefix = "Fire ";
                }
                else
                {
                    LootPrefix = "Enchanted ";
                }
                string[] Type = { "Sword", "Axe", "Bow", "Hammer", "Helm", "Chest", "Legs", "Boots" };
                string LootType = Type[random.Next(Type.Length)];
                if (Chance2 <= 60)
                {
                    LootSuffix = null;
                }
                else if (Chance2 <= 80)
                {
                    LootSuffix = " of Eridin";
                }
                else if (Chance2 <= 90)
                {
                    LootSuffix = " of Brahma";
                }
                else if (Chance2 <= 99)
                {
                    LootSuffix = " of Night and Day";
                }
                else
                {
                    LootSuffix = " of Destiny 2.0";
                }
                Loot = LootPrefix + LootType + LootSuffix;
            }
            return Loot;
        }

        public string Make150Loot(string channel, string user, int chance)
        {
            int Chance1 = random.Next(1, 101);
            int Chance2 = random.Next(1, 101);
            string LootPrefix;
            string LootSuffix;
            string Loot;
            if (GetRing(channel, user).Contains("Spoils"))
            {
                Chance1 = random.Next(30, 101);
                Chance2 = random.Next(30, 101);
            }
            if (chance >= random.Next(1, 101))
            {
                if (Chance1 <= 75)
                {
                    LootPrefix = "Enchanted ";
                }
                else if (Chance1 <= 99)
                {
                    LootPrefix = "Vibranium ";
                }
                else
                {
                    LootPrefix = "Infinity ";
                }
                string[] Type = { "Sword", "Axe", "Bow", "Hammer", "Helm", "Chest", "Legs", "Boots" };
                string LootType = Type[random.Next(Type.Length)];
                if (Chance2 <= 50)
                {
                    LootSuffix = " of Eridin";
                }
                else if (Chance2 <= 75)
                {
                    LootSuffix = " of Brahma";
                }
                else if (Chance2 <= 90)
                {
                    LootSuffix = " of Night and Day";
                }
                else
                {
                    LootSuffix = " of Destiny 2.0";
                }
                Loot = LootPrefix + LootType + LootSuffix;
            }
            else
            {
                if (Chance1 <= 60)
                {
                    LootPrefix = "Fire ";
                }
                else if (Chance1 <= 88)
                {
                    LootPrefix = "Enchanted ";
                }
                else if (Chance1 <= 99)
                {
                    LootPrefix = "Vibranium ";
                }
                else
                {
                    LootPrefix = "Infinity ";
                }
                string[] Type = { "Sword", "Axe", "Bow", "Hammer", "Helm", "Chest", "Legs", "Boots" };
                string LootType = Type[random.Next(Type.Length)];
                if (Chance2 <= 60)
                {
                    LootSuffix = null;
                }
                else if (Chance2 <= 80)
                {
                    LootSuffix = " of Eridin";
                }
                else if (Chance2 <= 90)
                {
                    LootSuffix = " of Brahma";
                }
                else if (Chance2 <= 99)
                {
                    LootSuffix = " of Night and Day";
                }
                else
                {
                    LootSuffix = " of Destiny 2.0";
                }
                Loot = LootPrefix + LootType + LootSuffix;
            }
            return Loot;
        }

        public string MakeHardLoot(string channel, string user)
        {
            int Chance1 = random.Next(1, 101);
            int Chance2 = random.Next(1, 101);
            string LootPrefix;
            string LootSuffix;
            if (GetRing(channel, user).Contains("Spoils"))
            {
                Chance1 = random.Next(50, 101);
                Chance2 = random.Next(50, 101);
            }
            if (Chance1 <= 60)
            {
                LootPrefix = "Steel ";
            }
            else if (Chance1 <= 95)
            {
                LootPrefix = "Moonstone ";
            }
            else if (Chance1 <= 99)
            {
                LootPrefix = "Fire ";
            }
            else
            {
                LootPrefix = "Enchanted ";
            }
            string[] Type = { "Sword", "Axe", "Bow", "Hammer", "Helm", "Chest", "Legs", "Boots" };
            string LootType = Type[random.Next(Type.Length)];
            if (Chance2 <= 60)
            {
                LootSuffix = null;
            }
            else if (Chance2 <= 80)
            {
                LootSuffix = " of Eridin";
            }
            else if (Chance2 <= 90)
            {
                LootSuffix = " of Brahma";
            }
            else if (Chance2 <= 99)
            {
                LootSuffix = " of Night and Day";
            }
            else
            {
                LootSuffix = " of Destiny 2.0";
            }
            string Loot = LootPrefix + LootType + LootSuffix;
            return Loot;
        }

        private void EndAdventure(string channel)
        {
            Console.WriteLine("1");
            string monster = null;
            XElement adv = XElement.Load(@"Data\Adventure.xml");
            foreach (XElement ele in adv.Descendants(channel + ".Monster"))
            {
                monster = ele.Attribute("Monster").Value.ToString();
            }
            if (GetMonsterHP(channel) <= 0)
            {
                Main.client.SendMessage(channel, "/me : " + monster + " has been defeated!");
            }
            string Msg = null;
            Console.WriteLine("2");
            foreach (string plyr in PlayerList[channel])
            {
                if (!IsRaid(channel) && !Is150(channel))
                {
                    GiveXP(channel, plyr);
                }
                string HP = null;
                string Dmg = null;
                XDocument Adv = XDocument.Load(@"Data\Adventure.xml");
                XElement ele = Adv.Element("adventure").Element("Channel." + channel).Element(channel + "." + plyr);
                Dmg = ele.Attribute("TotalDmg").Value.ToString();
                if (GetAdvPlayerHP(channel, plyr) <= 0)
                {
                    HP = "0";
                }
                else
                {
                    HP = GetAdvPlayerHP(channel, plyr).ToString();
                }
                Msg = Msg + plyr.ToLower() + " did " + Dmg + " dmg and had " + HP + " HP left. ";
            }
            Console.WriteLine("3");
            System.Threading.Thread.Sleep(200);
            Main.client.SendMessage(channel, "/me : " + Msg);
            String Player = PlayerList[channel][random.Next(PlayerList[channel].Count)];
            if (IsHard(channel))
            {
                if ((GetPlayerLoo(channel, Player) / 50 + 1) * 1000 >= random.Next(1, 4001))
                {
                    GiveLoot(channel, Player);
                }
            }
            else if (IsRaid(channel))
            {
                if (GetMonsterHP(channel) <= 0)
                {
                    foreach (string plyr in PlayerList[channel])
                    {
                        GiveXP(channel, plyr);
                        GiveRaidLoot(channel, plyr);
                    }
                }
            }
            else if (Is150(channel))
            {
                if (GetMonsterHP(channel) <= 0)
                {
                    foreach (string plyr in PlayerList[channel])
                    {
                        GiveXP(channel, plyr);
                        Give150Loot(channel, plyr);
                    }
                }
            }
            else
            {
                if ((GetPlayerLoo(channel, Player) / 50 + 1) * 1000 >= random.Next(1, 5001))
                {
                    GiveLoot(channel, Player);
                }
            }
            Console.WriteLine("4");
            if (random.Next(1, 4) == 3)
            {
                if (IsHard(channel) || IsRaid(channel) || Is150(channel))
                {
                    if (GetMonsterHP(channel) <= 0)
                    {
                        GiveCoin(channel, PlayerList[channel][random.Next(PlayerList[channel].Count)]);
                    }
                }
                else
                {
                    GiveCoin(channel, PlayerList[channel][random.Next(PlayerList[channel].Count)]);
                }
            }
            Console.WriteLine("5");
            System.Threading.Thread.Sleep(5000);
            System.Threading.Thread.Sleep(5000);
            Main.client.SendMessage(channel, "/me : Clearing the bodies...");
            System.Threading.Thread.Sleep(5000);
            if (IsRaid(channel))
            {
                Main.client.SendMessage(channel, "/me : Raid Finished!");
            }
            else
            {
                Main.client.SendMessage(channel, "/me : Adventure Finished!");
            }
            ClearPlayers(channel);
        }

        public void ClearPlayers(string channel)
        {
            try
            {
                lock (Main._lockadv)
                {
                    XDocument Adv = XDocument.Load(@"Data\Adventure.xml");
                    XElement C = Adv.Element("adventure").Element("Channel." + channel);
                    C.RemoveAll();
                    Adv.Save(@"Data\Adventure.xml");
                }
            }
            catch
            {

            }
            CheckChannel(channel);
            lock (Main._lockadv)
            {
                XDocument Adv2 = XDocument.Load(@"Data\Adventure.xml");
                XElement C2 = Adv2.Element("adventure").Element("Channel." + channel);
                C2.SetAttributeValue("Status", "Off");
                Adv2.Save(@"Data\Adventure.xml");
            }
            if (PlayerList.ContainsKey(channel))
            {
                PlayerList[channel].Clear();
            }
        }
    }
}
