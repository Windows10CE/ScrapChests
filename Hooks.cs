using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RoR2;
using RoR2.Artifacts;
using R2API;
using ScrapChests.Compat;
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
                    r2chancedetour.Apply();
                    On.RoR2.Run.BuildDropTable += DropTableHook;
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
                    r2chancedetour.Undo();
                    On.RoR2.Run.BuildDropTable -= DropTableHook;
                    On.RoR2.ChestBehavior.RollItem -= RollItemHook;
                    On.RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager.GenerateAvailableItemsSet -= EvolutionItemListHook;
                    On.RoR2.ArenaMissionController.OnStartServer -= VoidFieldsStartHook;
                    ScrapChestsPlugin._currentlyHooked = false;
                }
            }
            orig(self);
        }

        private static IDetour r2chancedetour = new Hook(typeof(ItemDropAPI).GetMethod("GetSelection", new Type[] { typeof(ItemDropLocation), typeof(float) }), ((Func<Func<ItemDropLocation, float, PickupIndex>, ItemDropLocation, float, PickupIndex>)R2APIChanceShrineHookHook).Method, new HookConfig
        {
            ManualApply = true
        });

        internal static PickupIndex R2APIChanceShrineHookHook(Func<ItemDropLocation, float, PickupIndex> orig, ItemDropLocation loc, float item)
        {
            PickupIndex r2apiChoice = orig(loc, item);

            if (r2apiChoice.pickupDef.equipmentIndex != EquipmentIndex.None || loc != ItemDropLocation.Shrine)
                return r2apiChoice;


            PickupIndex newIndex = PickupIndex.none;

            while (newIndex == PickupIndex.none)
            {
                WeightedSelection<PickupIndex> select = new WeightedSelection<PickupIndex>(4);
                select.AddChoice(Run.instance.treasureRng.NextElementUniform<PickupIndex>(Run.instance.availableTier1DropList), 0.36f);
                select.AddChoice(Run.instance.treasureRng.NextElementUniform<PickupIndex>(Run.instance.availableTier2DropList), 0.09f);
                select.AddChoice(Run.instance.treasureRng.NextElementUniform<PickupIndex>(Run.instance.availableTier3DropList), 0.01f);
                select.AddChoice(PickupIndex.none, 0.54f);
                newIndex = select.Evaluate(Run.instance.treasureRng.nextNormalizedFloat);
            }
            return newIndex;
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
            ScrapChestsPlugin._cachedItemLists[4] = toInsert;
            self.availableBossDropList.Clear();
            self.availableBossDropList.Add(PickupCatalog.FindPickupIndex(ItemIndex.ScrapYellow));

            if (ScrapChestsPlugin.LunarScrapEnabled)
            {
                toInsert = new List<PickupIndex>();
                self.availableLunarDropList.ForEach(x => toInsert.Add(x));
                ScrapChestsPlugin._cachedItemLists[3] = toInsert;
                self.availableLunarDropList.Clear();
                self.availableLunarDropList.Add(PickupCatalog.FindPickupIndex(LunarScrapCompat.GetLunarScrapIndex()));
            }
            else
            {
                ScrapChestsPlugin._cachedItemLists[4] = Run.instance.availableLunarDropList;
            } 
        }

        private static readonly FieldInfo[] DropListFields = new FieldInfo[]
        {
            typeof(Run).GetField("availableTier1DropList"),
            typeof(Run).GetField("availableTier2DropList"),
            typeof(Run).GetField("availableTier3DropList"),
            typeof(Run).GetField("availableLunarDropList"),
            typeof(Run).GetField("availableBossDropList")
        };

        internal static void ShopTerminalGenPickupHook(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            
            for (int i = 0; i < 5; i++)
            {
                int local = 0;
                var found = c.TryGotoNext(MoveType.Before,
                    x => x.MatchCallOrCallvirt(typeof(Run).GetProperty("instance").GetGetMethod()),
                    x => x.MatchLdfld(DropListFields[i]),
                    x => x.MatchStloc(out local)
                );
                if (found && ((ItemTier)i != ItemTier.Lunar || ScrapChestsPlugin.LunarScrapEnabled))
                {
                    c.Index += 2;
                    c.Emit(OpCodes.Ldarg_0);
                    c.Emit(OpCodes.Ldc_I4, i);
                    c.EmitDelegate<Func<List<PickupIndex>, ShopTerminalBehavior, int, List<PickupIndex>>>((currentList, shop, tier) =>
                    {
                        if (!ScrapChestsPlugin._currentlyHooked)
                            return currentList;
                        if (ScrapChestsPlugin._exceptionList.Any(x => shop.name.Contains(x)))
                            return ScrapChestsPlugin._cachedItemLists[tier];
                        return new List<PickupIndex> { PickupCatalog.FindPickupIndex(ItemCatalog.itemDefs.First(x => x.tier == (ItemTier)tier && x.ContainsTag(ItemTag.Scrap)).itemIndex) };
                    });
                }
            }
        }

        internal static void RollItemHook(On.RoR2.ChestBehavior.orig_RollItem orig, ChestBehavior self)
        {
            self.requiredItemTag = ItemTag.Any;
            orig(self);
        }

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
