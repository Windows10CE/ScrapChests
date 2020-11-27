using System;
using System.Collections.Generic;
using System.Linq;
using R2API.Utils;
using RoR2;
using UnityEngine.Networking;

namespace ScrapChests
{
    internal static class Hooks
    {
        internal static void RunStartHook(On.RoR2.Run.orig_Start orig, Run self)
        {
            if (RunArtifactManager.instance.IsArtifactEnabled(ArtifactOfDebris.DebrisArtifact.def.artifactIndex))
            {
                if (!ScrapChestsPlugin._currentlyHooked)
                {
                    On.RoR2.Run.BuildDropTable += DropTableHook;
                    On.RoR2.ShopTerminalBehavior.Start += ShopTerminalHook;
                    On.RoR2.ChestBehavior.RollItem += RollItemHook;
                    On.RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager.EnsureMonsterTeamItemCount += EvolutionHook;
                    ScrapChestsPlugin._currentlyHooked = true;
                }
            }
            else
            {
                if (ScrapChestsPlugin._currentlyHooked)
                {
                    On.RoR2.Run.BuildDropTable -= DropTableHook;
                    On.RoR2.ShopTerminalBehavior.Start -= ShopTerminalHook;
                    On.RoR2.ChestBehavior.RollItem -= RollItemHook;
                    On.RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager.EnsureMonsterTeamItemCount -= EvolutionHook;
                    ScrapChestsPlugin._currentlyHooked = false;
                }
            }
            orig(self);
        }

        internal static void DropTableHook(On.RoR2.Run.orig_BuildDropTable orig, Run self)
        {
            orig(self);
            List<PickupIndex> toInsert = new List<PickupIndex>();

            self.availableTier1DropList.ForEach(x => toInsert.Add(x));
            ScrapChestsPlugin._cachedItemLists.Add(toInsert);
            self.availableTier1DropList.Clear();
            self.availableTier1DropList.Add(PickupCatalog.FindPickupIndex(ItemIndex.ScrapWhite));

            toInsert = new List<PickupIndex>();
            self.availableTier2DropList.ForEach(x => toInsert.Add(x));
            ScrapChestsPlugin._cachedItemLists.Add(toInsert);
            self.availableTier2DropList.Clear();
            self.availableTier2DropList.Add(PickupCatalog.FindPickupIndex(ItemIndex.ScrapGreen));

            toInsert = new List<PickupIndex>();
            self.availableTier3DropList.ForEach(x => toInsert.Add(x));
            ScrapChestsPlugin._cachedItemLists.Add(toInsert);
            self.availableTier3DropList.Clear();
            self.availableTier3DropList.Add(PickupCatalog.FindPickupIndex(ItemIndex.ScrapRed));

            toInsert = new List<PickupIndex>();
            self.availableBossDropList.ForEach(x => toInsert.Add(x));
            ScrapChestsPlugin._cachedItemLists.Add(toInsert);
            self.availableBossDropList.Clear();
            self.availableBossDropList.Add(PickupCatalog.FindPickupIndex(ItemIndex.ScrapYellow));
        }

        internal static void ShopTerminalHook(On.RoR2.ShopTerminalBehavior.orig_Start orig, ShopTerminalBehavior self)
        {
            if (NetworkServer.active)
            {
                PickupIndex index = PickupIndex.none;
                if (!self.dropTable)
                {
                    List<List<PickupIndex>> itemList = ScrapChestsPlugin._exceptionList.Any(x => self.name.Contains(x)) ? ScrapChestsPlugin._cachedItemLists : new List<List<PickupIndex>> {
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
        }

        internal static void RollItemHook(On.RoR2.ChestBehavior.orig_RollItem orig, ChestBehavior self)
        {
            self.requiredItemTag = ItemTag.Any;
            orig(self);
        }

        internal static void EvolutionHook(On.RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager.orig_EnsureMonsterTeamItemCount orig, int arg)
        {
            return;
        }
    }
}
