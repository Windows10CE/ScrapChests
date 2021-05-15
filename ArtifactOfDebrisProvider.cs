using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using ModCommon;
using RoR2;
using RoR2.ContentManagement;
using UnityEngine;

namespace ScrapChests
{
    public class ArtifactOfDebrisProvider : IContentPackProvider
    {
        public string identifier => "ScrapChests";

        public static AssetBundle DebrisBundle { get; private set; }
        public static ContentPack ContentPack { get; } = new ContentPack();
        public static ArtifactDef DebrisArtifact { get; private set; }

        public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
        {
            DebrisBundle = AssetBundle.LoadFromMemory(Properties.Resources.debrisartifact);

            DebrisArtifact = ScriptableObject.CreateInstance<ArtifactDef>();

            LanguageTokens.Add("DEB_NAME_TOKEN", "Artifact of Debris");
            LanguageTokens.Add("DEB_DESC_TOKEN", "All items turn to scrap to be printed.");

            DebrisArtifact.nameToken = "DEB_NAME_TOKEN";
            DebrisArtifact.descriptionToken = "DEB_DESC_TOKEN";
            DebrisArtifact.smallIconDeselectedSprite = DebrisBundle.LoadAsset<Sprite>("Assets/Scrap_Dim.png");
            DebrisArtifact.smallIconSelectedSprite = DebrisBundle.LoadAsset<Sprite>("Assets/Scrap.png");

            ContentPack.artifactDefs.Add(new ArtifactDef[] { DebrisArtifact });

            args.ReportProgress(1f);
            yield break;
        }

        public IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
        {
            ContentPack.Copy(ContentPack, args.output);
            args.ReportProgress(1f);
            yield break;
        }

        public IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
        {
            args.ReportProgress(1f);
            yield break;
        }
    }
}
