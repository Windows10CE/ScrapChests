using RoR2;
using R2API;
using UnityEngine;
using System.Collections.Generic;

namespace ScrapChests.ArtifactOfDebris
{
    public static class DebrisArtifact
    {
        public static ArtifactDef def;

        internal static void Init()
        {
            def = ScriptableObject.CreateInstance<ArtifactDef>();

            LanguageAPI.Add("DEB_NAME_TOKEN", "Artifact of Debris");
            LanguageAPI.Add("DEB_DESC_TOKEN", "All items turn to scrap to be printed.");

            def.nameToken = "DEB_NAME_TOKEN";
            def.descriptionToken = "DEB_DESC_TOKEN";
            def.smallIconDeselectedSprite = Resources.Load<Sprite>("@Debris:Assets/Scrap_Dim.png");
            def.smallIconSelectedSprite = Resources.Load<Sprite>("@Debris:Assets/Scrap.png");

            ArtifactCatalog.getAdditionalEntries += AddDebrisArtifact;

            On.RoR2.Run.Start += Hooks.RunStartHook;
        }

        private static void AddDebrisArtifact(List<ArtifactDef> list)
        {
            list.Add(def);
        }
    }
}
