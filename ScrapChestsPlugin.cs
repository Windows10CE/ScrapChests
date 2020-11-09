using System;
using System.Collections.Generic;
using BepInEx;
using RoR2;
using R2API.Utils;
using UnityEngine.Networking;

namespace Windows10CE
{
    [BepInDependency("com.bepis.r2api", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin("com.Windows10CE.ScrapChests", "ScrapChests", "1.0.0")]
    public class ScrapChests : BaseUnityPlugin
    {
        private static Random _rand = new Random();
        private static List<List<PickupIndex>> _cachedItemLists = new List<List<PickupIndex>>();

        public void Awake()
        {
            On.RoR2.Run.BuildDropTable += (orig, self) =>
            {
                orig(self);
                List<PickupIndex> toInsert = new List<PickupIndex>();
                self.availableTier1DropList.ForEach(x => toInsert.Add(x));
                _cachedItemLists.Add(toInsert);
                self.availableTier1DropList.Clear();
                self.availableTier1DropList.Add(PickupCatalog.FindPickupIndex(ItemIndex.ScrapWhite));
                toInsert = new List<PickupIndex>();
                self.availableTier2DropList.ForEach(x => toInsert.Add(x));
                _cachedItemLists.Add(toInsert);
                self.availableTier2DropList.Clear();
                self.availableTier2DropList.Add(PickupCatalog.FindPickupIndex(ItemIndex.ScrapGreen));
                toInsert = new List<PickupIndex>();
                self.availableTier3DropList.ForEach(x => toInsert.Add(x));
                _cachedItemLists.Add(toInsert);
                self.availableTier3DropList.Clear();
                self.availableTier3DropList.Add(PickupCatalog.FindPickupIndex(ItemIndex.ScrapRed));
                toInsert = new List<PickupIndex>();
                self.availableBossDropList.ForEach(x => toInsert.Add(x));
                _cachedItemLists.Add(toInsert);
                self.availableBossDropList.Clear();
                self.availableBossDropList.Add(PickupCatalog.FindPickupIndex(ItemIndex.ScrapYellow));
            };
            On.RoR2.ShopTerminalBehavior.Start += (orig, self) =>
            {
                if (NetworkServer.active)
                {
                    List<PickupIndex> list;
                    switch (self.itemTier)
                    {
                        case ItemTier.Tier1:
                            list = _cachedItemLists[0];
                            break;
                        case ItemTier.Tier2:
                            list = _cachedItemLists[1];
                            break;
                        case ItemTier.Tier3:
                            list = _cachedItemLists[2];
                            break;
                        case ItemTier.Lunar:
                            list = Run.instance.availableLunarDropList;
                            break;
                        case ItemTier.Boss:
                            list = _cachedItemLists[3];
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    self.SetPickupIndex(list[_rand.Next(0, list.Count)]);
                }
                if (NetworkClient.active)
                {
                    self.InvokeMethod("UpdatePickupDisplayAndAnimations");
                }
            };
        }
    }
}
