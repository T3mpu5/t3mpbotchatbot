using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client;
using System.IO;
using TwitchLib.Api.Helix.Models.Extensions.Transactions;

namespace t3mpbotchatbot
{
    class Shop
    {
        Random random = new Random(Guid.NewGuid().GetHashCode());

        private void CheckOutlaws(string topic)
        {
            lock (Main._lockoutl)
            {
                XDocument Out = XDocument.Load(@"Data\Outlaws.xml");
                var ele = Out.Element("outlaws").Elements(topic).SingleOrDefault();
                if (ele == null)
                {
                    Out.Element("outlaws").Add(new XElement(topic));
                    Out.Save(@"Data\Outlaws.xml");
                }
            }
        }

        public void Refresh()
        {
            Adventure adventure = new Adventure();
            XDocument Shop = XDocument.Load(@"Data\Shop.xml");
            XElement ele = Shop.Element("shop").Element("Global.Shop");
            string date = ele.Attribute("LastDate").Value.ToString();
            string slot1 = RandomLoot();
            string cost1 = GetCost(slot1);
            string slot2;
            string cost2;
            string slot3;
            string cost3;
            List<int> list = new List<int>() { 21, 22, 23, 24, 25, 26, 27, 28, 29, 30 };
            if (random.Next(1, 11) == 5)
            {
                slot2 = "Loot Bag Upgrade (+1 Slot)";
                cost2 = "2500";
            }
            else
            {
                slot2 = RandomLoot();
                cost2 = GetCost(slot2);
            }
            if (random.Next(1, 4) == 3)
            {
                slot3 = "Rift Token";
                cost3 = "30";
            }
            else if (random.Next(1,101) == 45)
            {
                slot3 = adventure.MakeRing(20);
                cost3 = "1000";
            }
            else if (list.Contains(random.Next(1,101)))
            {
                slot3 = adventure.MakeRaidLoot("T3MPBOT", "T3MPBOT", 100);
                cost3 = "1000";
            }
            else if (random.Next(1,101) == 33)
            {
                slot3 = "Mysterious Sword of Unknown Power";
                cost3 = "1500";
            }
            else
            {
                slot3 = RandomLoot();
                cost3 = GetCost(slot3);
            }
            if (DateTime.Today > DateTime.Parse(date))
            {
                BackUp();
                XDocument Refresh = XDocument.Load(@"Data\Shop.xml");
                XElement shop = Refresh.Element("shop").Element("Global.Shop");
                shop.SetAttributeValue("Slot1", slot1);
                shop.SetAttributeValue("Slot2", slot2);
                shop.SetAttributeValue("Slot3", slot3);
                shop.SetAttributeValue("LastDate", DateTime.Today);
                shop.SetAttributeValue("Cost1", cost1);
                shop.SetAttributeValue("Cost2", cost2);
                shop.SetAttributeValue("Cost3", cost3);
                Refresh.Save(@"Data\Shop.xml");
            }
        }

        private void BackUp()
        {
            string sourcePath = @"Data\";
            string targetPath = @"DataBackUp\" + DateTime.Today.ToShortDateString().Replace('/','.') + @"\";
            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
            }
            foreach (var srcPath in Directory.GetFiles(sourcePath))
            {
                //Copy the file from sourcepath and place into mentioned target path, 
                //Overwrite the file if same file is exist in target path
                File.Copy(srcPath, srcPath.Replace(sourcePath, targetPath), true);
            }
        }

        private string GetCost(string slot)
        {
            int position = slot.IndexOf(" ");
            string type = slot.Substring(0, position).ToUpper();
            string item;
            if (slot.ToUpper().Contains("sword".ToUpper()))
            {
                item = "sword";
            }
            else
            {
                item = "armour";
            }
            XDocument Cost = XDocument.Load(@"Data\LootData.xml");
            XElement getCost = Cost.Element("loot").Element("costs").Element(item);
            string cost = getCost.Attribute(type).Value.ToString();
            if (slot.ToUpper().Contains("enlightenment".ToUpper()))
            {
                cost = Convert.ToString(Math.Ceiling(Convert.ToInt32(cost) * 1.1));
            }
            else if (slot.ToUpper().Contains("hidden".ToUpper()))
            {
                cost = Convert.ToString(Math.Ceiling(Convert.ToInt32(cost) * 1.2));
            }
            else if (slot.ToUpper().Contains("lost".ToUpper()))
            {
                cost = Convert.ToString(Math.Ceiling(Convert.ToInt32(cost) * 1.4));
            }
            return cost;
        }

        private string RandomLoot()
        {
            int chance1 = random.Next(1, 101);
            int chance2 = random.Next(1, 101);
            string[] items = { "Sword", "Helm", "Chest", "Legs", "Boots" };
            int index = random.Next(items.Length);
            string prefix = chance1 <= 1 ? "Paper " : chance1 <= 12 ? "Red " : chance1 <= 24 ? "Blue " : chance1 <= 30 ? "Glass " : chance1 <= 40 ? "Ice " : chance1 <= 50 ? "Leather " : chance1 <= 60 ? "Brass " : chance1 <= 70 ? "Dark " : chance1 <= 80 ? "Golden " : chance1 <= 90 ? "Ebony " : chance1 <= 97 ? "Dragon Bone " : "Spirit ";
            string item = items[index];
            string suffix = chance2 <= 70 ? null : chance2 <= 85 ? " of Enlightenment" : chance2 <= 95 ? " of Hidden Mysteries" : " of Lost Souls";
            string loot = prefix + item + suffix;
            return loot;
        }

        public string GetSlotCoins(string channel, string user)
        {
            Adventure adventure = new Adventure();
            int Coins;
            int Slot;
            if (adventure.GetLootSlot1(channel, user) != null && adventure.GetLootSlot1(channel, user).Contains("Coins"))
            {
                string[] Slot1 = adventure.GetLootSlot1(channel, user).Split(' ');
                Coins = Convert.ToInt32(Slot1[0]);
                Slot = 1;
            }
            else if (adventure.GetLootSlot2(channel, user) != null && adventure.GetLootSlot2(channel, user).Contains("Coins"))
            {
                string[] Slot2 = adventure.GetLootSlot2(channel, user).Split(' ');
                Coins = Convert.ToInt32(Slot2[0]);
                Slot = 2;
            }
            else if (adventure.GetLootSlot3(channel, user) != null && adventure.GetLootSlot3(channel, user).Contains("Coins"))
            {
                string[] Slot3 = adventure.GetLootSlot3(channel, user).Split(' ');
                Coins = Convert.ToInt32(Slot3[0]);
                Slot = 3;
            }
            else if (adventure.GetLootSlot4(channel, user) != null && adventure.GetLootSlot4(channel, user).Contains("Coins"))
            {
                string[] Slot4 = adventure.GetLootSlot4(channel, user).Split(' ');
                Coins = Convert.ToInt32(Slot4[0]);
                Slot = 4;
            }
            else if (adventure.GetLootSlot5(channel, user) != null && adventure.GetLootSlot5(channel, user).Contains("Coins"))
            {
                string[] Slot5 = adventure.GetLootSlot5(channel, user).Split(' ');
                Coins = Convert.ToInt32(Slot5[0]);
                Slot = 5;
            }
            else
            {
                Coins = 0;
                Slot = 0;
            }
            return Coins.ToString() + "." + Slot.ToString();
        }

        public int GetMaxBagSlots(string channel, string user)
        {
            XDocument Loot = XDocument.Load(@"Data\Loot.xml");
            XElement P = Loot.Element("loot").Element(Main.GetTopic(channel, user));
            string Max = null;
            try
            {
                Max = P.Attribute("MaxSlots").Value;
            }
            catch
            {

            }
            if (Max == null)
            {
                Max = "5";
            }
            return Convert.ToInt32(Max);
        }

        public bool IsBagFull(string channel, string user)
        {
            Adventure adventure = new Adventure();
            int i = 1;
            int s = 1;
            bool BagFull = true;
            Dictionary<int, string> methods = new Dictionary<int, string>();
            while (s <= GetMaxBagSlots(channel, user))
            {
                methods.Add(s, adventure.GetLootSlotN(channel, user, s));
                s++;
            }
            while (i <= GetMaxBagSlots(channel, user))
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

        public void BuyShopItem(string channel, string user, int Slot)
        {
            Adventure adventure = new Adventure();
            string[] CS = GetSlotCoins(channel, user).Split('.');
            XDocument Shop = XDocument.Load(@"Data\Shop.xml");
            XElement S = Shop.Element("shop").Element("Global.Shop");
            string Item = S.Attribute("Slot" + Slot).Value.ToString();
            int Cost = Convert.ToInt32(S.Attribute("Cost" + Slot).Value);
            adventure.CheckLootUser(channel, user);
            if (Item.Contains("Token"))
            {
                XDocument Loot = XDocument.Load(@"Data\Loot.xml");
                XElement P = Loot.Element("loot").Element(Main.GetTopic(channel, user));
                if (Convert.ToInt32(CS[0]) < Cost)
                {
                    Main.client.SendMessage(channel, "/me : You can't afford this item");
                }
                else
                {
                    lock (Main._lockxp)
                    {
                        XDocument XP = XDocument.Load(@"Data\XP.xml");
                        XElement P2 = XP.Element("users").Element(Main.GetTopic(channel, user));
                        int Token = 0;
                        try
                        {
                            Token = Convert.ToInt32(P2.Attribute("Tokens").Value);
                        }
                        catch
                        {

                        }
                        P2.SetAttributeValue("Tokens", Token + 1);
                        XP.Save(@"Data\XP.xml");
                        Main.client.SendMessage(channel, "/me : " + user.ToLower() + " bought " + Item);
                    }
                    lock (Main._lockloot)
                    {
                        if (Convert.ToInt32(CS[0]) == Cost)
                        {
                            P.SetAttributeValue("Slot" + CS[1], null);
                        }
                        else
                        {
                            P.SetAttributeValue("Slot" + CS[1], Convert.ToInt32(CS[0]) - Cost + " Gold Coins");
                        }
                    Loot.Save(@"Data\Loot.xml");
                    }
                }
            }
            else if (Item.Contains("Upgrade"))
            {
                XDocument Loot = XDocument.Load(@"Data\Loot.xml");
                XElement P = Loot.Element("loot").Element(Main.GetTopic(channel, user));
                if (Convert.ToInt32(CS[0]) < Cost)
                {
                    Main.client.SendMessage(channel, "/me : You can't afford this item");
                }
                else if (!CanBuyUpgrade(channel, user))
                {
                    Main.client.SendMessage(channel, "/me : You have already purchased this today!");
                }
                else
                {
                    lock (Main._lockloot)
                    {
                        P.SetAttributeValue("MaxSlots", GetMaxBagSlots(channel, user) + 1);
                        if (Convert.ToInt32(CS[0]) == Cost)
                        {
                            P.SetAttributeValue("Slot" + CS[1], null);
                        }
                        else
                        {
                            P.SetAttributeValue("Slot" + CS[1], Convert.ToInt32(CS[0]) - Cost + " Gold Coins");
                        }
                        Loot.Save(@"Data\Loot.xml");
                    }
                    lock (Main._lockxp)
                    {
                        XDocument XP = XDocument.Load(@"Data\XP.xml");
                        XElement P2 = XP.Element("users").Element(Main.GetTopic(channel, user));
                        P2.SetAttributeValue("BuyDate", DateTime.Now.Date);
                        XP.Save(@"Data\XP.xml");
                        Main.client.SendMessage(channel, "/me : " + user.ToLower() + " bought " + Item);
                    }
                }
            }
            else
            {
                lock (Main._lockloot)
                {
                    XDocument Loot = XDocument.Load(@"Data\Loot.xml");
                    XElement P = Loot.Element("loot").Element(Main.GetTopic(channel, user));
                    if (Convert.ToInt32(CS[0]) < Cost)
                    {
                        Main.client.SendMessage(channel, "/me : You can't afford this item");
                    }
                    else
                    {
                        if (adventure.GetLootSlot1(channel, user) == null)
                        {
                            P.SetAttributeValue("Slot1", Item);
                            if (Convert.ToInt32(CS[0]) == Cost)
                            {
                                P.SetAttributeValue("Slot" + CS[1], null);
                            }
                            else
                            {
                                P.SetAttributeValue("Slot" + CS[1], Convert.ToInt32(CS[0]) - Cost + " Gold Coins");
                            }
                            Main.client.SendMessage(channel, "/me : " + user.ToLower() + " bought " + Item);
                        }
                        else if (adventure.GetLootSlot2(channel, user) == null)
                        {
                            P.SetAttributeValue("Slot2", Item);
                            if (Convert.ToInt32(CS[0]) == Cost)
                            {
                                P.SetAttributeValue("Slot" + CS[1], null);
                            }
                            else
                            {
                                P.SetAttributeValue("Slot" + CS[1], Convert.ToInt32(CS[0]) - Cost + " Gold Coins");
                            }
                            Main.client.SendMessage(channel, "/me : " + user.ToLower() + " bought " + Item);
                        }
                        else if (adventure.GetLootSlot3(channel, user) == null)
                        {
                            P.SetAttributeValue("Slot3", Item);
                            if (Convert.ToInt32(CS[0]) == Cost)
                            {
                                P.SetAttributeValue("Slot" + CS[1], null);
                            }
                            else
                            {
                                P.SetAttributeValue("Slot" + CS[1], Convert.ToInt32(CS[0]) - Cost + " Gold Coins");
                            }
                            Main.client.SendMessage(channel, "/me : " + user.ToLower() + " bought " + Item);
                        }
                        else if (adventure.GetLootSlot4(channel, user) == null)
                        {
                            P.SetAttributeValue("Slot4", Item);
                            if (Convert.ToInt32(CS[0]) == Cost)
                            {
                                P.SetAttributeValue("Slot" + CS[1], null);
                            }
                            else
                            {
                                P.SetAttributeValue("Slot" + CS[1], Convert.ToInt32(CS[0]) - Cost + " Gold Coins");
                            }
                            Main.client.SendMessage(channel, "/me : " + user.ToLower() + " bought " + Item);
                        }
                        else if (adventure.GetLootSlot5(channel, user) == null)
                        {
                            P.SetAttributeValue("Slot5", Item);
                            if (Convert.ToInt32(CS[0]) == Cost)
                            {
                                P.SetAttributeValue("Slot" + CS[1], null);
                            }
                            else
                            {
                                P.SetAttributeValue("Slot" + CS[1], Convert.ToInt32(CS[0]) - Cost + " Gold Coins");
                            }
                            Main.client.SendMessage(channel, "/me : " + user.ToLower() + " bought " + Item);
                            Main.client.SendWhisper(user, "Your loot bag is now full, make sure to throw something away before your next adventure!");
                        }
                        else
                        {
                            Main.client.SendMessage(channel, "/me : An error occured!");
                        }
                    }
                    Loot.Save(@"Data\Loot.xml");
                }
            }
        }

        public bool CanBuyUpgrade(string channel, string user)
        {
            UpdateOutlaws(channel, user);
            lock (Main._lockxp)
            {
                XDocument XP = XDocument.Load(@"Data\XP.xml");
                XElement P = XP.Element("users").Element(Main.GetTopic(channel, user));
                string BDate = null;
                bool CanBuy = true;
                try
                {
                    BDate = P.Attribute("BuyDate").Value;
                }
                catch
                {

                }
                if (BDate != null && DateTime.Parse(BDate) == DateTime.Now.Date)
                {
                    CanBuy = false;
                }
                return CanBuy;
            }
        }

        public bool CanSteal(string channel, string user)
        {
            UpdateOutlaws(channel, user);
            lock (Main._lockxp)
            {
                XDocument XP = XDocument.Load(@"Data\XP.xml");
                XElement P = XP.Element("users").Element(Main.GetTopic(channel, user));
                string Outlaw = null;
                string SDate = null;
                bool CanSteal = true;
                try
                {
                    SDate = P.Attribute("StealDate").Value;
                }
                catch
                {

                }
                string Topic;
                if (Main.GetTopic(channel, user).Contains("GLOBAL"))
                {
                    Topic = "GLOBAL.Outlaws";
                }
                else
                {
                    Topic = channel + ".Outlaws";
                }
                XDocument Out = XDocument.Load(@"Data\Outlaws.xml");
                XElement P2 = Out.Element("outlaws").Element(Topic);
                foreach (XAttribute O in P2.Attributes())
                {
                    if (O.Name == user)
                    {
                        Outlaw = "Yes";
                    }
                }
                if (Outlaw != null && Outlaw == "Yes")
                {
                    CanSteal = false;
                }
                if (SDate != null && DateTime.Parse(SDate) == DateTime.Now.Date)
                {
                    CanSteal = false;
                }
                return CanSteal;
            }
        }

        public void AddOutlaw(string channel, string user)
        {
            string Topic;
            if (Main.GetTopic(channel, user).Contains("GLOBAL"))
            {
                Topic = "GLOBAL.Outlaws";
            }
            else
            {
                Topic = channel + ".Outlaws";
            }
            CheckOutlaws(Topic);
            lock (Main._lockoutl)
            {
                XDocument Outlaw = XDocument.Load(@"Data\Outlaws.xml");
                XElement P = Outlaw.Element("outlaws").Element(Topic);
                P.SetAttributeValue(user, DateTime.Now);
                XElement P2 = Outlaw.Element("outlaws");
                P2.Add(new XElement(Main.GetTopic(channel, user)));
                Outlaw.Save(@"Data\Outlaws.xml");
            }
        }

        public void RemoveOutlaw(string channel, string user)
        {
            string Topic;
            if (Main.GetTopic(channel, user).Contains("GLOBAL"))
            {
                Topic = "GLOBAL.Outlaws";
            }
            else
            {
                Topic = channel + ".Outlaws";
            }
            CheckOutlaws(Topic);
            lock (Main._lockoutl)
            {
                XDocument Outlaw = XDocument.Load(@"Data\Outlaws.xml");
                XElement P = Outlaw.Element("outlaws").Element(Topic);
                XElement P2 = Outlaw.Element("outlaws").Element(Main.GetTopic(channel, user));
                P.SetAttributeValue(user, null);
                P2.RemoveAll();
                Outlaw.Save(@"Data\Outlaws.xml");
            }
        }

        public void ListOutlaws(string channel, string user)
        {
            UpdateOutlaws(channel, user);
            string Topic;
            if (Main.GetTopic(channel, user).Contains("GLOBAL"))
            {
                Topic = "GLOBAL.Outlaws";
            }
            else
            {
                Topic = channel + ".Outlaws";
            }
            XDocument OL = XDocument.Load(@"Data\Outlaws.xml");
            XElement P = OL.Element("outlaws").Element(Topic);
            string Msg = null;
            foreach (XAttribute atr in P.Attributes())
            {
                Msg = Msg + atr.Name.ToString().ToLower() + ", ";
            }
            if (Msg == null)
            {
                Msg = "There are no outlaws right now!";
            }
            else
            {
                Msg = "The current outlaws are: " + Msg;
            }
            Main.client.SendMessage(channel, "/me : " + Msg.TrimEnd(' ', ','));
        }

        private void UpdateOutlaws(string channel, string user)
        {
            string Topic;
            if (Main.GetTopic(channel, user).Contains("GLOBAL"))
            {
                Topic = "GLOBAL.Outlaws";
            }
            else
            {
                Topic = channel + ".Outlaws";
            }
            CheckOutlaws(Topic);
            XDocument OL = XDocument.Load(@"Data\Outlaws.xml");
            XElement P = OL.Element("outlaws").Element(Topic);
            foreach (XAttribute atr in P.Attributes())
            {
                if (DateTime.Parse(atr.Value) < DateTime.Now.AddDays(-1))
                {
                    RemoveOutlaw(channel, atr.Name.ToString());
                }
            }
        }

        public void TrySteal(string channel, string user)
        {
            System.Threading.Thread.Sleep(3000);
            XDocument Shop = XDocument.Load(@"Data\Shop.xml");
            XElement S = Shop.Element("shop").Element("Global.Shop");
            lock (Main._lockxp)
            {
                XDocument XP = XDocument.Load(@"Data\XP.xml");
                XElement P = XP.Element("users").Element(Main.GetTopic(channel, user));
                string item = "Token";
                while (item.Contains("Token"))
                {
                    item = S.Attribute("Slot" + random.Next(1, 3)).Value.ToString();
                }
                if (random.Next(1, 100) >= 93)
                {
                    AddOutlaw(channel, user);
                    P.SetAttributeValue("StolenLoot", item);
                    GiveStolenItem(channel, user, item);
                    Main.client.SendMessage(channel, "/me : " + user.ToLower() + " stole " + item + " and is now an OUTLAW! Bring them to justice to earn some coin.");
                }
                else
                {
                    Main.client.SendMessage(channel, "/me : " + user.ToLower() + " tried to steal " + item + " and FAILED!");
                }
                P.SetAttributeValue("StealDate", DateTime.Now.Date);
                XP.Save(@"Data\XP.xml");
            }
        }

        private void GiveStolenItem(string channel, string user, string item)
        {
            Adventure adventure = new Adventure();
            adventure.CheckLootUser(channel, user);
            lock (Main._lockloot)
            {
                XDocument Loot = XDocument.Load(@"Data\Loot.xml");
                XElement L = Loot.Element("loot").Element(Main.GetTopic(channel, user));
                L.SetAttributeValue(adventure.GetNextLootSlot(channel, user), item);
                Loot.Save(@"Data\Loot.xml");
            }
        }

        public bool CheckOutlaw(string channel, string user)
        {
            UpdateOutlaws(channel, user);
            string Topic;
            if (Main.GetTopic(channel, user).Contains("GLOBAL"))
            {
                Topic = "GLOBAL.Outlaws";
            }
            else
            {
                Topic = channel + ".Outlaws";
            }
            XDocument OL = XDocument.Load(@"Data\Outlaws.xml");
            XElement P = OL.Element("outlaws").Element(Topic);
            bool IsOutlaw = false;
            foreach (XAttribute atr in P.Attributes())
            {
                if (atr.Name.ToString() == user)
                {
                    IsOutlaw = true;
                    break;
                }
            }
            return IsOutlaw;
        }

        public void TryKill(string channel, string killer, string target)
        {
            System.Threading.Thread.Sleep(1500);
            UpdateOutlaws(channel, killer);
            string Topic = Main.GetTopic(channel, target);
            XDocument OL = XDocument.Load(@"Data\Outlaws.xml");
            XElement T = OL.Element("outlaws").Element(Topic);
            bool Tried = false;
            foreach (XAttribute atr in T.Attributes())
            {
                if (atr.Name.ToString() == killer)
                {
                    Tried = true;
                }
            }
            if (Tried)
            {
                Main.client.SendMessage(channel, "/me : " + killer.ToLower() + ", you have already tried to kill " + target.ToLower() + ".");
            }
            else if (random.Next(1, 11) == 10)
            {
                int Coins = random.Next(1, Main.LookUpLvl(channel, target));
                Main.client.SendMessage(channel, "/me : " + killer.ToLower() + " has killed " + target.ToLower() + " and recieved " + Coins + " Gold Coins");
                GiveKillCoin(channel, killer, Coins);
                RemoveKillLoot(channel, target);
                System.Threading.Thread.Sleep(300);
                RemoveOutlaw(channel, target);
                UpdateOutlaws(channel, killer);
            }
            else
            {
                lock (Main._lockoutl)
                {
                    XDocument OL2 = XDocument.Load(@"Data\Outlaws.xml");
                    XElement T2 = OL2.Element("outlaws").Element(Topic);
                    Main.client.SendMessage(channel, "/me : " + killer.ToLower() + ", you failed to kill " + target.ToLower() + "! They are still at large!");
                    T2.SetAttributeValue(killer, "X");
                    OL2.Save(@"Data\Outlaws.xml");
                }
            }
        }

        private void GiveKillCoin(string channel, string user, int coins)
        {
            Adventure adventure = new Adventure();
            adventure.CheckLootUser(channel, user);
            lock (Main._lockloot)
            {
                XDocument loot = XDocument.Load(@"Data\Loot.xml");
                XElement P = loot.Element("loot").Element(Main.GetTopic(channel, user));
               if (adventure.GetLootSlot1(channel, user).Contains("Coins"))
                {
                    P.SetAttributeValue("Slot1", (coins + Convert.ToInt32(adventure.GetLootSlot1(channel, user).Substring(0, adventure.GetLootSlot1(channel, user).IndexOf(" ")))) + " Gold Coins");
                }
                else if (adventure.GetLootSlot2(channel, user).Contains("Coins"))
                {
                    P.SetAttributeValue("Slot2", (coins + Convert.ToInt32(adventure.GetLootSlot2(channel, user).Substring(0, adventure.GetLootSlot2(channel, user).IndexOf(" ")))) + " Gold Coins");
                }
                else if (adventure.GetLootSlot3(channel, user).Contains("Coins"))
                {
                    P.SetAttributeValue("Slot3", (coins + Convert.ToInt32(adventure.GetLootSlot3(channel, user).Substring(0, adventure.GetLootSlot3(channel, user).IndexOf(" ")))) + " Gold Coins");
                }
                else if (adventure.GetLootSlot4(channel, user).Contains("Coins"))
                {
                    P.SetAttributeValue("Slot4", (coins + Convert.ToInt32(adventure.GetLootSlot4(channel, user).Substring(0, adventure.GetLootSlot4(channel, user).IndexOf(" ")))) + " Gold Coins");
                }
                else if (adventure.GetLootSlot5(channel, user).Contains("Coins"))
                {
                    P.SetAttributeValue("Slot5", (coins + Convert.ToInt32(adventure.GetLootSlot5(channel, user).Substring(0, adventure.GetLootSlot5(channel, user).IndexOf(" ")))) + " Gold Coins");
                }
                else if (adventure.GetLootSlot1(channel, user) == null)
                {
                    P.SetAttributeValue("Slot1", coins + " Gold Coins");
                }
                else if (adventure.GetLootSlot2(channel, user) == null)
                {
                    P.SetAttributeValue("Slot2", coins + " Gold Coins");
                }
                else if (adventure.GetLootSlot3(channel, user) == null)
                {
                    P.SetAttributeValue("Slot3", coins + " Gold Coins");
                }
                else if (adventure.GetLootSlot4(channel, user) == null)
                {
                    P.SetAttributeValue("Slot4", coins + " Gold Coins");
                }
                else if (adventure.GetLootSlot5(channel, user) == null)
                {
                    P.SetAttributeValue("Slot5", coins + " Gold Coins");
                }
                else
                {
                    Main.client.SendMessage(channel, "/me : Whoops, bag was full!");
                }
                loot.Save(@"Data\Loot.xml");
            }
        }

        private void RemoveKillLoot(string channel, string user)
        {
            Adventure adventure = new Adventure();
            XDocument XP = XDocument.Load(@"Data\XP.xml");
            XElement P = XP.Element("users").Element(Main.GetTopic(channel, user));
            string item = P.Attribute("StolenLoot").Value;
            string type;
            adventure.CheckLootUser(channel, user);
            lock (Main._lockloot)
            {
                XDocument Loot = XDocument.Load(@"Data\Loot.xml");
                XElement P2 = Loot.Element("loot").Element(Main.GetTopic(channel, user));
                if (item.Contains("Sword"))
                {
                    type = "sword!";
                }
                else if (item.Contains("Helm"))
                {
                    type = "helm!";
                }
                else if (item.Contains("Chest"))
                {
                    type = "chest!";
                }
                else if (item.Contains("Legs"))
                {
                    type = "legs!";
                }
                else if (item.Contains("Boots"))
                {
                    type = "boots!";
                }
                else
                {
                    type = item;
                }
                if (adventure.GetWeapon(channel, user) == item)
                {
                    P2.SetAttributeValue("Sword", null);
                    Main.client.SendMessage(channel, "/me : " + user.ToLower() + " lost their stolen " + type);
                }
                else if (adventure.GetHelm(channel, user) == item)
                {
                    P2.SetAttributeValue("Helm", null);
                    Main.client.SendMessage(channel, "/me : " + user.ToLower() + " lost their stolen " + type);
                }
                else if (adventure.GetChest(channel, user) == item)
                {
                    P2.SetAttributeValue("Chest", null);
                    Main.client.SendMessage(channel, "/me : " + user.ToLower() + " lost their stolen " + type);
                }
                else if (adventure.GetLegs(channel, user) == item)
                {
                    P2.SetAttributeValue("Legs", null);
                    Main.client.SendMessage(channel, "/me : " + user.ToLower() + " lost their stolen " + type);
                }
                else if (adventure.GetBoots(channel, user) == item)
                {
                    P2.SetAttributeValue("Boots", null);
                    Main.client.SendMessage(channel, "/me : " + user.ToLower() + " lost their stolen " + type);
                }
                else if (adventure.GetLootSlot1(channel, user) == item)
                {
                    P2.SetAttributeValue("Slot1", null);
                    Main.client.SendMessage(channel, "/me : " + user.ToLower() + " lost their stolen " + type);
                }
                else if (adventure.GetLootSlot2(channel, user) == item)
                {
                    P2.SetAttributeValue("Slot2", null);
                    Main.client.SendMessage(channel, "/me : " + user.ToLower() + " lost their stolen " + type);
                }
                else if (adventure.GetLootSlot3(channel, user) == item)
                {
                    P2.SetAttributeValue("Slot3", null);
                    Main.client.SendMessage(channel, "/me : " + user.ToLower() + " lost their stolen " + type);
                }
                else if (adventure.GetLootSlot4(channel, user) == item)
                {
                    P2.SetAttributeValue("Slot4", null);
                    Main.client.SendMessage(channel, "/me : " + user.ToLower() + " lost their stolen " + type);
                }
                else if (adventure.GetLootSlot5(channel, user) == item)
                {
                    P2.SetAttributeValue("Slot5", null);
                    Main.client.SendMessage(channel, "/me : " + user.ToLower() + " lost their stolen " + type);
                }
                else
                {
                    Main.client.SendMessage(channel, "/me : " + user.ToLower() + " no longer has their stolen " + type);
                }
                Loot.Save(@"Data.Loot.xml");
            }
        }
    }
}
