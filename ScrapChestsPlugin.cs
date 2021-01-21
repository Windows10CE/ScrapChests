using System.Collections.Generic;
using BepInEx;
using RoR2;
using R2API;
using R2API.Utils;
using System;

namespace ScrapChests
{   
    [BepInPlugin(ModGuid, ModName, ModVer)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]

    [BepInDependency(R2API.R2API.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [R2APISubmoduleDependency(new string[] { nameof(ResourcesAPI), nameof(LanguageAPI) })]
    [BepInDependency(LunarScrap.LunarScrapPlugin.ModGuid, BepInDependency.DependencyFlags.SoftDependency)]
    public class ScrapChestsPlugin : BaseUnityPlugin
    {
        public const string ModName = "ScrapChests";
        public const string ModVer = "3.2.3";
        public const string ModGuid = "com.Windows10CE.ScrapChests";

        public static ScrapChestsPlugin Instance;
        internal static BepInEx.Logging.ManualLogSource logSource;

        public static List<PickupIndex>[] _cachedItemLists = new List<PickupIndex>[5];
        public static string[] _exceptionList { get; internal set; } = { "Duplicator", "LunarCauldron" };
        public static bool _currentlyHooked { get; internal set; }

        internal static bool LunarScrapEnabled { get; private set; }

        public void Awake()
        {
            Instance = this;
            logSource = Instance.Logger;

            AddBundleProvider("@Debris", Properties.Resources.debris);

            LunarScrapEnabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(LunarScrap.LunarScrapPlugin.ModGuid);

            ArtifactOfDebris.DebrisArtifact.Init();
        }

        private void AddBundleProvider(string prefix, byte[] bundleBytes)
        {
            if (bundleBytes is null) throw new ArgumentNullException(nameof(bundleBytes));
            if (prefix is null || !prefix.StartsWith("@")) throw new ArgumentException("Invalid bundle prefix", nameof(prefix));
            
            var bundle = UnityEngine.AssetBundle.LoadFromMemory(bundleBytes);
            if (bundle is null) throw new NullReferenceException($"{nameof(bundleBytes)} was not a valid assetbundle.");

            var provider = new AssetBundleResourcesProvider(prefix, bundle);

            ResourcesAPI.AddProvider(provider);
        }
    }
}
