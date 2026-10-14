using System.Linq;
using AntColony.Ants;
using AntColony.Commands;
using AntColony.PlayerUI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AntColony.Tests.EditMode
{
    public sealed class ColonyManagementEditModeTests
    {
        private const string HudPrefabPath =
            "Assets/AntColony/Prefabs/UI/PlayerHud.prefab";
        private const string RowPrefabPath =
            "Assets/AntColony/Prefabs/UI/WorkPriorityRow.prefab";

        [Test]
        public void AuthoredManagementUiUsesExistingHudAndReusableRows()
        {
            GameObject hud = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            GameObject row = AssetDatabase.LoadAssetAtPath<GameObject>(RowPrefabPath);

            Assert.That(hud, Is.Not.Null);
            Assert.That(row, Is.Not.Null);
            Assert.That(hud.GetComponentsInChildren<ColonyManagementView>(true),
                Has.Length.EqualTo(1));
            Assert.That(row.GetComponent<WorkPriorityRowView>(), Is.Not.Null);
            Assert.That(hud.GetComponentsInChildren<Canvas>(true), Has.Length.EqualTo(1));
        }

        [Test]
        public void MatrixOnlyOffersCategoriesWithRealExecutors()
        {
            Assert.That(ColonyManagementView.SupportedCategories,
                Is.EqualTo(new[]
                {
                    AntWorkCategory.Excavation,
                    AntWorkCategory.Construction,
                    AntWorkCategory.Hauling
                }));
            Assert.That(ColonyManagementView.SupportedCategories,
                Has.None.EqualTo(AntWorkCategory.SurfacePreparation));
            Assert.That(ColonyManagementView.SupportedCategories,
                Has.None.EqualTo(AntWorkCategory.Foraging));
        }

        [Test]
        public void RecordPriorityApiSupportsOneThroughFourAndDisabledIndependently()
        {
            GameObject root = new("Priority API Test");
            try
            {
                AntSelectionCommandController controller =
                    root.AddComponent<AntSelectionCommandController>();
                AntRecord mara = CreateRecord("mara", "Mara");
                AntRecord pip = CreateRecord("pip", "Pip");
                int originalSkill = mara.Skills.Get(AntSkillId.Excavation).Level;

                AntWorkPriority[] values =
                {
                    AntWorkPriority.Highest,
                    AntWorkPriority.High,
                    AntWorkPriority.Normal,
                    AntWorkPriority.Low,
                    AntWorkPriority.Disabled
                };
                foreach (AntWorkPriority value in values)
                {
                    Assert.That(controller.TrySetWorkPriority(
                        mara, AntWorkCategory.Excavation, value, out string message),
                        Is.True, message);
                    Assert.That(mara.WorkPriorities.Get(AntWorkCategory.Excavation),
                        Is.EqualTo(value));
                }

                Assert.That(pip.WorkPriorities.Get(AntWorkCategory.Excavation),
                    Is.EqualTo(AntWorkPriority.Normal));
                Assert.That(mara.Skills.Get(AntSkillId.Excavation).Level,
                    Is.EqualTo(originalSkill));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static AntRecord CreateRecord(string id, string name)
        {
            return new AntRecord(new AntId(id), name, new ColonyId("test-colony"),
                AntCaste.Worker, new AntAttributes(5, 5, 5));
        }
    }
}
