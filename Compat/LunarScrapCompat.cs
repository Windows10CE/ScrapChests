using RoR2;

namespace ScrapChests.Compat
{
    public static class LunarScrapCompat
    {
        public static ItemIndex GetLunarScrapIndex() => LunarScrap.LunarScrap.LunarScrapIndex;

        public static bool IsLunarDropTable(this PickupDropTable table) => table is LunarScrap.LunarPrinter.LunarDropTable;
    }
}
