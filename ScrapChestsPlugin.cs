using System;
using System.Collections.Generic;
using BepInEx;
using RoR2;
using R2API.Utils;
using UnityEngine.Networking;
using HarmonyLib;
using System.Reflection.Emit;
using System.Linq;

namespace Windows10CE
{
    [BepInDependency("com.bepis.r2api", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin("com.Windows10CE.ScrapChests", "ScrapChests", "1.0.0")]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod)]
    public class ScrapChests : BaseUnityPlugin
    {
        private static List<List<PickupIndex>> _cachedItemLists = new List<List<PickupIndex>>();

        public void Awake()
        {
            // Harmony.CreateAndPatchAll(System.Reflection.Assembly.GetExecutingAssembly(), "com.Windows10CE.ScrapChests");

            On.RoR2.Run.BuildDropTable += (orig, self) =>
            {
                orig(self);

                List<PickupIndex> toInsert = new List<PickupIndex>();

                self.availableTier1DropList.ForEach(x => toInsert.Add(x));
                _cachedItemLists.Add(toInsert);
                self.availableTier1DropList.Clear();
                self.availableTier1DropList.Add(PickupCatalog.FindPickupIndex(ItemIndex.ScrapWhite));

                toInsert = new List<PickupIndex>();
                self.availableTier2DropList.ForEach(x => toInsert.Add(x));
                _cachedItemLists.Add(toInsert);
                self.availableTier2DropList.Clear();
                self.availableTier2DropList.Add(PickupCatalog.FindPickupIndex(ItemIndex.ScrapGreen));

                toInsert = new List<PickupIndex>();
                self.availableTier3DropList.ForEach(x => toInsert.Add(x));
                _cachedItemLists.Add(toInsert);
                self.availableTier3DropList.Clear();
                self.availableTier3DropList.Add(PickupCatalog.FindPickupIndex(ItemIndex.ScrapRed));

                toInsert = new List<PickupIndex>();
                self.availableBossDropList.ForEach(x => toInsert.Add(x));
                _cachedItemLists.Add(toInsert);
                self.availableBossDropList.Clear();
                self.availableBossDropList.Add(PickupCatalog.FindPickupIndex(ItemIndex.ScrapYellow));
            };
            On.RoR2.ShopTerminalBehavior.Start += (orig, self) =>
            {
                if (NetworkServer.active)
                {
                    List<List<PickupIndex>> itemList = self.name.Contains("Duplicator") ? _cachedItemLists : new List<List<PickupIndex>> { Run.instance.availableTier1DropList, Run.instance.availableTier2DropList, Run.instance.availableTier3DropList, Run.instance.availableBossDropList };
                    List <PickupIndex> list;
                    switch (self.itemTier)
                    {
                        case ItemTier.Tier1:
                            list = itemList[0];
                            break;
                        case ItemTier.Tier2:
                            list = itemList[1];
                            break;
                        case ItemTier.Tier3:
                            list = itemList[2];
                            break;
                        case ItemTier.Lunar:
                            list = Run.instance.availableLunarDropList;
                            break;
                        case ItemTier.Boss:
                            list = itemList[3];
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    self.SetPickupIndex(list[UnityEngine.Random.Range(0, list.Count)]);
                }
                if (NetworkClient.active)
                {
                    self.InvokeMethod("UpdatePickupDisplayAndAnimations");
                }
            };
            On.RoR2.ItemDef.ContainsTag += (_, _, _) =>
            {
                return true;
            };
            /*On.RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager.GrantMonsterTeamItem += (orig) =>
            {
                var list = _cachedItemLists[0].Select(x => PickupCatalog.GetPickupDef(x).itemIndex).ToArray();
                Traverse.Create(typeof(RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager)).Field("availableTier1Items").SetValue(list);
                list = _cachedItemLists[1].Select(x => PickupCatalog.GetPickupDef(x).itemIndex).ToArray();
                Traverse.Create(typeof(RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager)).Field("availableTier2Items").SetValue(list);
                list = _cachedItemLists[2].Select(x => PickupCatalog.GetPickupDef(x).itemIndex).ToArray();
                Traverse.Create(typeof(RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager)).Field("availableTier3Items").SetValue(list);
            };*/
            On.RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager.EnsureMonsterTeamItemCount += (_, _) =>
            {
                return;
            };
        }

        /*[HarmonyPatch(typeof(RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager), MethodType.Constructor)]
        class ArtifactsPatch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return new CodeMatcher(instructions)
                    .MatchForward(false,
                        new CodeMatch(OpCodes.Ldc_I4_4),
                        new CodeMatch(OpCodes.Newarr, typeof(ItemTag)),
                        new CodeMatch(OpCodes.Dup),
                        new CodeMatch(OpCodes.Ldtoken),
                        new CodeMatch(OpCodes.Call),
                        new CodeMatch(OpCodes.Stsfld)
                    )
                    .RemoveInstructions(6)
                    .InsertAndAdvance(
                        new CodeInstruction(OpCodes.Ldc_I4_0),
                        new CodeInstruction(OpCodes.Newarr, typeof(ItemTag)),
                        new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager), "forbiddenTags"))
                    )
                    .InstructionEnumeration();
            }
        }*/
    }
}
