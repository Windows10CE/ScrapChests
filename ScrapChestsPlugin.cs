using System;
using System.Collections.Generic;
using BepInEx;
using RoR2;
using R2API.Utils;
using UnityEngine.Networking;
using System.Linq;

namespace Windows10CE
{   
    [BepInDependency("com.bepis.r2api", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin("com.Windows10CE.ScrapChests", "ScrapChests", "1.0.0")]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod)]
    public class ScrapChests : BaseUnityPlugin
    {
        private static List<List<PickupIndex>> _cachedItemLists = new List<List<PickupIndex>>();
        private static string[] _exceptionList = { "Duplicator", "LunarCauldron" };

        public void Awake()
        {
            // Harmony.CreateAndPatchAll(System.Reflection.Assembly.GetExecutingAssembly(), "com.Windows10CE.ScrapChests");

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
                    PickupIndex index = PickupIndex.none;
                    if (!self.dropTable)
                    {
                        List<List<PickupIndex>> itemList = _exceptionList.Any(x => self.name.Contains(x)) ? _cachedItemLists : new List<List<PickupIndex>> {
                            Run.instance.availableTier1DropList,
                            Run.instance.availableTier2DropList,
                            Run.instance.availableTier3DropList,
                            Run.instance.availableBossDropList
                        };
                        List<PickupIndex> list;
                        switch (self.itemTier)
                        {
                            case ItemTier.Tier1:
                                list = itemList[0];
                                break;
                            case ItemTier.Tier2:
                                list = itemList[1];
                                break;
                            case ItemTier.Tier3:
                                list = itemList[2];
                                break;
                            case ItemTier.Lunar:
                                list = Run.instance.availableLunarDropList;
                                break;
                            case ItemTier.Boss:
                                list = itemList[3];
                                break;
                            default:
                                throw new Exception("Shop given with no dropTable and no itemTier, can't continue...");
                        }
                        index = list[UnityEngine.Random.Range(0, list.Count - 1)];
                    }
                    else
                        index = self.dropTable.GenerateDrop(Run.instance.treasureRng);
                    self.SetPickupIndex(index);
                }
                if (NetworkClient.active)
                {
                    self.InvokeMethod("UpdatePickupDisplayAndAnimations");
                }
            };
            On.RoR2.ChestBehavior.RollItem += (orig, self) =>
            {
                self.requiredItemTag = ItemTag.Any;
                orig(self);
            };
            On.RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager.EnsureMonsterTeamItemCount += (_, _) =>
            {
                return;
            };
        }
    }
}
