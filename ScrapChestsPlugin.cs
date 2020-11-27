using System.Collections.Generic;
using BepInEx;
using RoR2;
using R2API.Utils;

namespace ScrapChests
{   
    [BepInPlugin(ModGuid, ModName, ModVer)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]

    [BepInDependency("com.bepis.r2api", BepInDependency.DependencyFlags.HardDependency)]
    [R2APISubmoduleDependency(new string[] { "ResourcesAPI", "LanguageAPI", "LoadoutAPI" })]
    public class ScrapChestsPlugin : BaseUnityPlugin
    {
        private const string ModName = "ScrapChests";
        private const string ModVer = "2.0.0";
        private const string ModGuid = "com.Windows10CE.ScrapChests";

        public static ScrapChestsPlugin Instance;
        internal static BepInEx.Logging.ManualLogSource logSource;

        internal static List<List<PickupIndex>> _cachedItemLists = new List<List<PickupIndex>>();
        internal static string[] _exceptionList = { "Duplicator", "LunarCauldron" };
        internal static bool _currentlyHooked;

        public void Awake()
        {
            Instance = this;
            logSource = Instance.Logger;

            ArtifactOfDebris.DebrisArtifact.Init();
        }
    }
}
