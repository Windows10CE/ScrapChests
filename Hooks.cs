using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using RoR2;
using RoR2.Artifacts;
using HarmonyLib;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace ScrapChests
{
    [HarmonyPatch]
    internal static class EnabledPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Run), nameof(Run.Start))]
        internal static void RunStartPrefix()
        {
            ScrapChestsPlugin.HarmonyInstance.UnpatchSelf();
            ScrapChestsPlugin.HarmonyInstance.PatchAll(typeof(EnabledPatch));
            if (RunArtifactManager.instance.IsArtifactEnabled(ArtifactOfDebrisProvider.DebrisArtifact))
            {
                ScrapChestsPlugin.HarmonyInstance.PatchAll(typeof(Patches));
                if (ScrapChestsPlugin.LunarScrapEnabled)
                    ScrapChestsPlugin.HarmonyInstance.PatchAll(typeof(Compat.LunarScrapCompat));
            }
        }
    }

    [HarmonyPatch]
    internal static class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Run), nameof(Run.BuildDropTable))]
        internal static void DropTablePostfix(ref Run __instance)
        {
            List<PickupIndex> toInsert = new List<PickupIndex>();

            __instance.availableTier1DropList.ForEach(x => toInsert.Add(x));
            ScrapChestsPlugin._cachedItemLists[0] = toInsert;
            __instance.availableTier1DropList.Clear();
            __instance.availableTier1DropList.Add(PickupCatalog.FindPickupIndex(RoR2Content.Items.ScrapWhite.itemIndex));

            toInsert = new List<PickupIndex>();
            __instance.availableTier2DropList.ForEach(x => toInsert.Add(x));
            ScrapChestsPlugin._cachedItemLists[1] = toInsert;
            __instance.availableTier2DropList.Clear();
            __instance.availableTier2DropList.Add(PickupCatalog.FindPickupIndex(RoR2Content.Items.ScrapGreen.itemIndex));

            toInsert = new List<PickupIndex>();
            __instance.availableTier3DropList.ForEach(x => toInsert.Add(x));
            ScrapChestsPlugin._cachedItemLists[2] = toInsert;
            __instance.availableTier3DropList.Clear();
            __instance.availableTier3DropList.Add(PickupCatalog.FindPickupIndex(RoR2Content.Items.ScrapRed.itemIndex));

            toInsert = new List<PickupIndex>();
            __instance.availableBossDropList.ForEach(x => toInsert.Add(x));
            ScrapChestsPlugin._cachedItemLists[4] = toInsert;
            __instance.availableBossDropList.Clear();
            __instance.availableBossDropList.Add(PickupCatalog.FindPickupIndex(RoR2Content.Items.ScrapYellow.itemIndex));

            if (ScrapChestsPlugin.LunarScrapEnabled)
            {
                toInsert = new List<PickupIndex>();
                __instance.availableLunarDropList.ForEach(x => toInsert.Add(x));
                ScrapChestsPlugin._cachedItemLists[3] = toInsert;
                __instance.availableLunarDropList.Clear();
                __instance.availableLunarDropList.Add(PickupCatalog.FindPickupIndex(Compat.LunarScrapCompat.GetLunarScrapDef().itemIndex));
            }
            else
            {
                ScrapChestsPlugin._cachedItemLists[3] = Run.instance.availableLunarDropList;
            }
        }

        private static readonly FieldInfo[] DropListFields = new FieldInfo[]
        {
            AccessTools.Field(typeof(Run), "availableTier1DropList"),
            AccessTools.Field(typeof(Run), "availableTier2DropList"),
            AccessTools.Field(typeof(Run), "availableTier3DropList"),
            AccessTools.Field(typeof(Run), "availableLunarDropList"),
            AccessTools.Field(typeof(Run), "availableBossDropList")
        };

        [HarmonyILManipulator]
        [HarmonyPatch(typeof(ShopTerminalBehavior), nameof(ShopTerminalBehavior.GenerateNewPickupServer))]
        internal static void ShopTerminalGenPickupIL(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            for (int i = 0; i < 5; i++)
            {
                int local = 0;
                var found = c.TryGotoNext(MoveType.Before,
                    x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(Run), nameof(Run.instance))),
                    x => x.MatchLdfld(DropListFields[i]),
                    x => x.MatchStloc(out local)
                );
                if (found && ((ItemTier)i != ItemTier.Lunar || ScrapChestsPlugin.LunarScrapEnabled))
                {
                    c.Index += 2;
                    c.Emit(OpCodes.Pop);
                    c.Emit(OpCodes.Ldarg_0);
                    c.Emit(OpCodes.Ldc_I4, i);
                    c.EmitDelegate<Func<ShopTerminalBehavior, int, List<PickupIndex>>>((shop, tier) =>
                    {
                        if (ScrapChestsPlugin.ExceptionList.Any(x => shop.name.Contains(x)))
                            return ScrapChestsPlugin._cachedItemLists[tier];
                        return new List<PickupIndex> { PickupCatalog.FindPickupIndex(ItemCatalog.itemDefs.First(x => x.tier == (ItemTier)tier && x.ContainsTag(ItemTag.Scrap)).itemIndex) };
                    });
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChestBehavior), nameof(ChestBehavior.RollItem))]
        internal static void RollItemPrefix(ref ChestBehavior __instance)
        {
            __instance.requiredItemTag = ItemTag.Any;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MonsterTeamGainsItemsArtifactManager), nameof(MonsterTeamGainsItemsArtifactManager.GenerateAvailableItemsSet))]
        internal static bool MonsterAvailableItemsGenPrefix()
        {
            ItemIndex[] GenerateDropList(List<PickupIndex> pickupsList)
            {
                return pickupsList
                    .Select(x => ItemCatalog.GetItemDef(PickupCatalog.GetPickupDef(x)?.itemIndex ?? ItemIndex.None))
                    .Where(x => x != null)
                    .Where(x => MonsterTeamGainsItemsArtifactManager.IsItemAllowedForMonsters(x))
                    .Select(x => x.itemIndex)
                    .ToArray();
            }

            MonsterTeamGainsItemsArtifactManager.availableTier1Items = GenerateDropList(ScrapChestsPlugin._cachedItemLists[0]);
            MonsterTeamGainsItemsArtifactManager.availableTier2Items = GenerateDropList(ScrapChestsPlugin._cachedItemLists[1]);
            MonsterTeamGainsItemsArtifactManager.availableTier3Items = GenerateDropList(ScrapChestsPlugin._cachedItemLists[2]);
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ArenaMissionController), nameof(ArenaMissionController.OnStartServer))]
        internal static void VoidFieldsStartPostfix(ref ArenaMissionController __instance)
        {
            __instance.availableTier1DropList = ScrapChestsPlugin._cachedItemLists[0].Where(x => ArenaMissionController.IsPickupAllowedForMonsters(x)).ToList();
            __instance.availableTier2DropList = ScrapChestsPlugin._cachedItemLists[1].Where(x => ArenaMissionController.IsPickupAllowedForMonsters(x)).ToList();
            __instance.availableTier3DropList = ScrapChestsPlugin._cachedItemLists[2].Where(x => ArenaMissionController.IsPickupAllowedForMonsters(x)).ToList();
        }
    }
}
