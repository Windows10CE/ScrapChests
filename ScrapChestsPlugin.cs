using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using RoR2;
using RoR2.ContentManagement;

namespace ScrapChests
{   
    [BepInPlugin(ModGuid, ModName, ModVer)]
    [ModCommon.NetworkModlistInclude]

    [BepInDependency(ModCommon.ModCommonPlugin.ModGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(LunarScrap.LunarScrapPlugin.ModGuid, BepInDependency.DependencyFlags.SoftDependency)]
    public class ScrapChestsPlugin : BaseUnityPlugin
    {
        public const string ModName = "ScrapChests";
        public const string ModVer = "4.0.0";
        public const string ModGuid = "com.Windows10CE.ScrapChests";

        new internal static ManualLogSource Logger;

        internal static Harmony HarmonyInstance = new Harmony(ModGuid);

        public static List<PickupIndex>[] _cachedItemLists = new List<PickupIndex>[5];
        public static string[] ExceptionList { get; internal set; } = { "Duplicator", "LunarCauldron" };
        
        internal static bool LunarScrapEnabled { get; private set; }

        public void Awake()
        {
            ScrapChestsPlugin.Logger = base.Logger;

            LunarScrapEnabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(LunarScrap.LunarScrapPlugin.ModGuid);

            ContentManager.collectContentPackProviders += (Add) => Add(new ArtifactOfDebrisProvider());

            HarmonyInstance.PatchAll(typeof(EnabledPatch));
        }
    }
}
