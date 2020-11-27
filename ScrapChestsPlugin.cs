using System.Collections.Generic;
using BepInEx;
using RoR2;
using R2API.Utils;

namespace ScrapChests
{   
    [BepInPlugin("com.Windows10CE.ScrapChests", "ScrapChests", "2.0.0")]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]

    [BepInDependency("com.bepis.r2api", BepInDependency.DependencyFlags.HardDependency)]
    [R2APISubmoduleDependency(new string[] { "ResourcesAPI", "LanguageAPI" })]
    public class ScrapChestsPlugin : BaseUnityPlugin
    {
        internal static List<List<PickupIndex>> _cachedItemLists = new List<List<PickupIndex>>();
        internal static string[] _exceptionList = { "Duplicator", "LunarCauldron" };

        public void Awake()
        {
            // Harmony.CreateAndPatchAll(System.Reflection.Assembly.GetExecutingAssembly(), "com.Windows10CE.ScrapChests");

            On.RoR2.Run.BuildDropTable += Hooks.DropTableHook;
            On.RoR2.ShopTerminalBehavior.Start += Hooks.ShopTerminalHook;
            On.RoR2.ChestBehavior.RollItem += Hooks.RollItemHook;
            On.RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager.EnsureMonsterTeamItemCount += Hooks.EvolutionHook;
        }
    }
}
