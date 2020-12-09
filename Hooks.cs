﻿using System;
using System.Collections.Generic;
using System.Linq;
using RoR2;
using RoR2.Artifacts;
using UnityEngine.Networking;

namespace ScrapChests
{
    internal static class Hooks
    {
        internal static void RunStartHook(On.RoR2.Run.orig_Start orig, Run self)
        {
            if (RunArtifactManager.instance.IsArtifactEnabled(ArtifactOfDebris.DebrisArtifact.def.artifactIndex))
            {
                ScrapChestsPlugin.logSource.LogMessage("Artifact of Debris enabled, hooking (or already hooked) required methods.");
                if (!ScrapChestsPlugin._currentlyHooked)
                {
                    On.RoR2.Run.BuildDropTable += DropTableHook;
                    On.RoR2.ShopTerminalBehavior.Start += ShopTerminalHook;
                    On.RoR2.ChestBehavior.RollItem += RollItemHook;
                    On.RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager.GenerateAvailableItemsSet += EvolutionItemListHook;
                    On.RoR2.ArenaMissionController.OnStartServer += VoidFieldsStartHook;
                    ScrapChestsPlugin._currentlyHooked = true;
                }
            }
            else
            {
                ScrapChestsPlugin.logSource.LogMessage("Artifact of Debris disabled, unhooking (or already unhooked) required methods.");
                if (ScrapChestsPlugin._currentlyHooked)
                {
                    On.RoR2.Run.BuildDropTable -= DropTableHook;
                    On.RoR2.ShopTerminalBehavior.Start -= ShopTerminalHook;
                    On.RoR2.ChestBehavior.RollItem -= RollItemHook;
                    On.RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager.GenerateAvailableItemsSet -= EvolutionItemListHook;
                    On.RoR2.ArenaMissionController.OnStartServer -= VoidFieldsStartHook;
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
            ScrapChestsPlugin._cachedItemLists[0] = toInsert;
            self.availableTier1DropList.Clear();
            self.availableTier1DropList.Add(PickupCatalog.FindPickupIndex(ItemIndex.ScrapWhite));

            toInsert = new List<PickupIndex>();
            self.availableTier2DropList.ForEach(x => toInsert.Add(x));
            ScrapChestsPlugin._cachedItemLists[1] = toInsert;
            self.availableTier2DropList.Clear();
            self.availableTier2DropList.Add(PickupCatalog.FindPickupIndex(ItemIndex.ScrapGreen));

            toInsert = new List<PickupIndex>();
            self.availableTier3DropList.ForEach(x => toInsert.Add(x));
            ScrapChestsPlugin._cachedItemLists[2] = toInsert;
            self.availableTier3DropList.Clear();
            self.availableTier3DropList.Add(PickupCatalog.FindPickupIndex(ItemIndex.ScrapRed));

            toInsert = new List<PickupIndex>();
            self.availableBossDropList.ForEach(x => toInsert.Add(x));
            ScrapChestsPlugin._cachedItemLists[3] = toInsert;
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
                    List<PickupIndex>[] itemList = ScrapChestsPlugin._exceptionList.Any(x => self.name.Contains(x)) ? ScrapChestsPlugin._cachedItemLists : new List<PickupIndex>[] {
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
                self.UpdatePickupDisplayAndAnimations();
            }
        }

        internal static void RollItemHook(On.RoR2.ChestBehavior.orig_RollItem orig, ChestBehavior self)
        {
            self.requiredItemTag = ItemTag.Any;
            orig(self);
        }

        /*
        internal static void EvolutionHook(On.RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager.orig_EnsureMonsterTeamItemCount orig, int arg)
        {
            return;
        }
        */

        internal static void EvolutionItemListHook(On.RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager.orig_GenerateAvailableItemsSet orig)
        {
            MonsterTeamGainsItemsArtifactManager.availableTier1Items = GenerateDropList(ScrapChestsPlugin._cachedItemLists[0]);
            MonsterTeamGainsItemsArtifactManager.availableTier2Items = GenerateDropList(ScrapChestsPlugin._cachedItemLists[1]);
            MonsterTeamGainsItemsArtifactManager.availableTier3Items = GenerateDropList(ScrapChestsPlugin._cachedItemLists[2]);
        }

        private static ItemIndex[] GenerateDropList(List<PickupIndex> pickupsList)
        {
            return pickupsList
                .Select(x => ItemCatalog.GetItemDef(PickupCatalog.GetPickupDef(x)?.itemIndex ?? ItemIndex.None))
                .Where(x => x is not null)
                .Where(x => MonsterTeamGainsItemsArtifactManager.IsItemAllowedForMonsters(x))
                .Select(x => x.itemIndex)
                .ToArray();
        }

        private static void VoidFieldsStartHook(On.RoR2.ArenaMissionController.orig_OnStartServer orig, ArenaMissionController self)
        {
            orig(self);
            self.availableTier1DropList = ScrapChestsPlugin._cachedItemLists[0].Where(x => ArenaMissionController.IsPickupAllowedForMonsters(x)).ToList();
            self.availableTier2DropList = ScrapChestsPlugin._cachedItemLists[1].Where(x => ArenaMissionController.IsPickupAllowedForMonsters(x)).ToList();
            self.availableTier3DropList = ScrapChestsPlugin._cachedItemLists[2].Where(x => ArenaMissionController.IsPickupAllowedForMonsters(x)).ToList();
        }
    }
}
