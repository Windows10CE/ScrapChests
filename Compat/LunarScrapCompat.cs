using RoR2;
using HarmonyLib;

namespace ScrapChests.Compat
{
    [HarmonyPatch]
    public static class LunarScrapCompat
    {
        public static ItemDef GetLunarScrapDef() => LunarScrap.LunarScrapProvider.LunarScrapDef;

        public static bool IsLunarDropTable(this PickupDropTable table) => table is LunarScrap.LunarPrinter.LunarDropTable;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LunarScrap.LunarPrinter.LunarDropTable), nameof(LunarScrap.LunarPrinter.LunarDropTable.GenerateDrop))]
        public static bool GenerateDropPrefix(ref Xoroshiro128Plus rng, ref PickupIndex __result)
        {
            __result = ScrapChestsPlugin._cachedItemLists[3][rng.RangeInt(0, ScrapChestsPlugin._cachedItemLists[3].Count)];
            return false;
        }
    }
}
