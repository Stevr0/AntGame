using System.Linq;
using AntColony.Commands;
using AntColony.Development;
using AntColony.PlayerUI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AntColony.Tests.EditMode
{
    public sealed class PlayerHudEditModeTests
    {
        private const string ScenePath =
            "Assets/AntColony/Scenes/AntGameDevelopment.unity";
        private const string HudPrefabPath =
            "Assets/AntColony/Prefabs/UI/PlayerHud.prefab";

        [TearDown]
        public void TearDown()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void PlayerHudPrefabIsAuthoredFromFocusedPresenters()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<Canvas>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<GraphicRaycaster>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<PlayerHudController>(), Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<AntRosterEntryView>(true),
                Has.Length.EqualTo(3));
            Assert.That(prefab.GetComponentsInChildren<SimulationControlsView>(true),
                Has.Length.EqualTo(1));
            Assert.That(prefab.GetComponentsInChildren<ColonyViewCommandBar>(true),
                Has.Length.EqualTo(1));
            Assert.That(prefab.GetComponentsInChildren<SelectedAntInspectorView>(true),
                Has.Length.EqualTo(1));
            Assert.That(prefab.GetComponentsInChildren<ColonyResourceSummaryView>(true),
                Has.Length.EqualTo(1));
            Assert.That(prefab.GetComponentsInChildren<ColonyManagementView>(true),
                Has.Length.EqualTo(1));
        }

        [Test]
        public void DevelopmentSceneHasOneHudAndOneEventSystemWithoutTemporaryStrip()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Assert.That(FindAll<PlayerHudController>(scene), Has.Length.EqualTo(1));
            Assert.That(FindAll<EventSystem>(scene), Has.Length.EqualTo(1));
            Assert.That(FindAll<SimulationTimeControlStrip>(scene), Is.Empty);
            AntSelectionCommandController controller =
                FindAll<AntSelectionCommandController>(scene).Single();
            Assert.That(controller.DevelopmentUiVisible, Is.False);
        }

        [Test]
        public void CommandControllerHonoursPlayerUiHitTestBoundary()
        {
            GameObject root = new("UI Boundary Test");
            try
            {
                AntSelectionCommandController controller =
                    root.AddComponent<AntSelectionCommandController>();
                controller.BindPlayerUiInputBlocker(new AlwaysBlockedInput());

                Assert.That(controller.IsScreenPointBlocked(new Vector2(100f, 200f)),
                    Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CancelToolDelegatesToExistingDesignationAuthority()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            AntGameDevelopmentWorldHost host =
                FindAll<AntGameDevelopmentWorldHost>(scene).Single();
            Assert.That(host.RebuildWorld(), Is.True);
            Vector2Int cell = AntGameDevelopmentWorldHost.ExcavationSoilFixture;
            Assert.That(host.SelectionController.TryDesignateExcavation(cell, out _),
                Is.True);

            int cancelled = host.SelectionController.CancelDesignationArea(
                cell, cell, out string summary);

            Assert.That(cancelled, Is.EqualTo(1), summary);
            Assert.That(host.ExcavationDesignations.Designations.Single().State,
                Is.EqualTo(ExcavationDesignationState.Cancelled));
        }

        private static T[] FindAll<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .ToArray();

        private sealed class AlwaysBlockedInput : IPlayerUiInputBlocker
        {
            public bool IsScreenPointBlocked(Vector2 screenPoint) => true;
        }
    }
}
