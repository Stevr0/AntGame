using System.Linq;
using AntColony.Ants;
using AntColony.Commands;
using AntColony.Development;
using AntColony.Navigation;
using AntColony.World;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AntColony.Tests.EditMode
{
    /// <summary>Focused input-independent checks for Milestone 3.1A.</summary>
    public sealed class AntSelectionCommandTests
    {
        private const string ScenePath =
            "Assets/AntColony/Scenes/AntGameDevelopment.unity";

        [TearDown]
        public void TearDown()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void ThreeSelectableAnts_AreSeparateDistinctAndShareNavigation()
        {
            AntGameDevelopmentWorldHost host = OpenHostWithSelectableAnts();

            Assert.That(host.SelectableGridAnts, Has.Count.EqualTo(3));
            Assert.That(host.SelectionController.SelectableAnts,
                Has.Count.EqualTo(3));
            Assert.That(
                host.SelectableGridAnts.Select(ant => ant.Position).Distinct().Count(),
                Is.EqualTo(3));
            Assert.That(host.SelectableGridAnts.All(ant =>
                    ant.NavigationWorld == host.NavigationWorldService),
                Is.True);

            Color[] colours = host.SelectableGridAnts
                .Select(ant => ant.GetComponentInChildren<SpriteRenderer>().color)
                .ToArray();
            Assert.That(colours.Distinct().Count(), Is.EqualTo(3));

            SelectableGridAnt[] selectable = host.SelectionController.SelectableAnts
                .ToArray();
            Assert.That(selectable.Select(ant => ant.Record.Id).Distinct().Count(),
                Is.EqualTo(3));
            Assert.That(selectable.Count(ant => ant.Record.Caste == AntCaste.Queen),
                Is.EqualTo(1));
            Assert.That(selectable.Count(ant => ant.Record.Caste == AntCaste.Worker),
                Is.EqualTo(2));
            Assert.That(selectable.Select(ant => ant.Record.Attributes.Strength)
                .Distinct().Count(), Is.EqualTo(3));
        }

        [Test]
        public void SelectionAndCommands_KeepTheCorrectAuthoritativeIdentity()
        {
            AntGameDevelopmentWorldHost host = OpenHostWithSelectableAnts();
            AntSelectionCommandController controller = host.SelectionController;
            SelectableGridAnt first = controller.SelectableAnts[0];
            SelectableGridAnt second = controller.SelectableAnts[1];
            AntId firstId = first.Record.Id;
            AntId secondId = second.Record.Id;

            controller.Select(first);
            Assert.That(controller.SelectedAnt.Record.Id, Is.EqualTo(firstId));
            Assert.That(controller.TryIssueMoveOrder(
                SampleWorldGenerator.GridDirectDestination), Is.True);
            controller.Select(second);
            Assert.That(controller.SelectedAnt.Record.Id, Is.EqualTo(secondId));
            Assert.That(first.Record.Id, Is.EqualTo(firstId));
            Assert.That(first.Motor.PendingRequestId, Is.GreaterThan(0));
            Assert.That(second.Motor.PendingRequestId, Is.EqualTo(0));
        }

        [Test]
        public void DeceasedAnt_RejectsNewMovementOrderAndKeepsItsRecord()
        {
            AntGameDevelopmentWorldHost host = OpenHostWithSelectableAnts();
            AntSelectionCommandController controller = host.SelectionController;
            SelectableGridAnt ant = controller.SelectableAnts[0];
            AntId id = ant.Record.Id;
            ant.Record.MarkDeceased();
            controller.Select(ant);

            Assert.That(controller.TryIssueMoveOrder(
                SampleWorldGenerator.GridDirectDestination), Is.False);
            Assert.That(ant.FailureReason, Does.Contain("Deceased"));
            Assert.That(ant.Motor.PendingRequestId, Is.EqualTo(0));
            Assert.That(host.AntRegistry.TryGet(id, out AntRecord retained), Is.True);
            Assert.That(retained, Is.SameAs(ant.Record));
        }

        [Test]
        public void DevelopmentAnts_StartWithIndependentZeroSkillSets()
        {
            AntGameDevelopmentWorldHost host = OpenHostWithSelectableAnts();
            AntSelectionCommandController controller = host.SelectionController;
            SelectableGridAnt mara = controller.SelectableAnts[0];
            SelectableGridAnt pip = controller.SelectableAnts[1];
            SelectableGridAnt tala = controller.SelectableAnts[2];

            Assert.That(controller.SelectableAnts.All(ant =>
                ant.Record.Skills.All.All(skill => skill.Level == 0)), Is.True);

            controller.Select(mara);
            Assert.That(controller.TryAwardSelectedSkillExperience(
                AntSkillId.Excavation, 250d, 1d, out _, out _), Is.True);
            controller.Select(pip);
            Assert.That(controller.TryAwardSelectedSkillExperience(
                AntSkillId.Construction, 350d, 1d, out _, out _), Is.True);
            controller.Select(tala);
            Assert.That(controller.TryAwardSelectedSkillExperience(
                AntSkillId.Hauling, 450d, 1d, out _, out _), Is.True);

            Assert.That(mara.Record.Skills.Get(AntSkillId.Excavation).Level,
                Is.GreaterThan(0));
            Assert.That(mara.Record.Skills.Get(AntSkillId.Construction).Level,
                Is.Zero);
            Assert.That(pip.Record.Skills.Get(AntSkillId.Construction).Level,
                Is.GreaterThan(0));
            Assert.That(tala.Record.Skills.Get(AntSkillId.Hauling).Level,
                Is.GreaterThan(0));
        }

        [Test]
        public void SelectionAndVisualRespawn_PreserveSkillOwnership()
        {
            AntGameDevelopmentWorldHost host = OpenHostWithSelectableAnts();
            AntSelectionCommandController controller = host.SelectionController;
            SelectableGridAnt mara = controller.SelectableAnts[0];
            AntRecord record = mara.Record;
            controller.Select(mara);
            Assert.That(controller.TryAwardSelectedSkillExperience(
                AntSkillId.Excavation, 500d, 1d, out _, out _), Is.True);
            double experience = record.Skills.Get(AntSkillId.Excavation)
                .TotalExperience;

            controller.Select(controller.SelectableAnts[1]);
            controller.Select(mara);
            Assert.That(mara.Record, Is.SameAs(record));
            Assert.That(host.SpawnOrResetSelectableGridAnts(out string message),
                Is.True, message);

            SelectableGridAnt rebound = host.SelectionController.SelectableAnts[0];
            Assert.That(rebound.Record, Is.SameAs(record));
            Assert.That(rebound.Record.Skills.Get(AntSkillId.Excavation)
                .TotalExperience, Is.EqualTo(experience));
        }

        [Test]
        public void DevelopmentWorldReset_RestoresCleanDeterministicSkills()
        {
            AntGameDevelopmentWorldHost host = OpenHostWithSelectableAnts();
            AntSelectionCommandController controller = host.SelectionController;
            controller.Select(controller.SelectableAnts[0]);
            Assert.That(controller.TryAwardSelectedSkillExperience(
                AntSkillId.Excavation, 50000d, 1d, out _, out _), Is.True);
            Assert.That(controller.TryAdvanceSkillInactivity(48d, out _), Is.True);

            Assert.That(host.RebuildWorld(), Is.True);
            Assert.That(host.SpawnOrResetSelectableGridAnts(out string message),
                Is.True, message);

            Assert.That(host.SelectionController.SkillSimulationHours, Is.Zero);
            Assert.That(host.SelectionController.SelectableAnts.All(ant =>
                ant.Record.Skills.All.All(skill => skill.Level == 0)), Is.True);
        }

        [Test]
        public void SelectionAndVisualRespawn_PreserveIndependentNeeds()
        {
            AntGameDevelopmentWorldHost host = OpenHostWithSelectableAnts();
            AntSelectionCommandController controller = host.SelectionController;
            SelectableGridAnt mara = controller.SelectableAnts[0];
            SelectableGridAnt pip = controller.SelectableAnts[1];
            AntRecord maraRecord = mara.Record;
            controller.Select(mara);
            Assert.That(controller.TrySetSelectedNeedsForDevelopment(
                90d, 40d, 70d, 60d, out _), Is.True);

            controller.Select(pip);
            Assert.That(pip.Record.Needs.Hunger, Is.EqualTo(100d));
            controller.Select(mara);
            Assert.That(host.SpawnOrResetSelectableGridAnts(out string message),
                Is.True, message);

            SelectableGridAnt rebound = host.SelectionController.SelectableAnts[0];
            Assert.That(rebound.Record, Is.SameAs(maraRecord));
            Assert.That(rebound.Record.Needs.Hunger, Is.EqualTo(40d));
            Assert.That(rebound.Record.Needs.Thirst, Is.EqualTo(70d));
        }

        [Test]
        public void DevelopmentWorldReset_RestoresDeterministicNeeds()
        {
            AntGameDevelopmentWorldHost host = OpenHostWithSelectableAnts();
            AntSelectionCommandController controller = host.SelectionController;
            controller.Select(controller.SelectableAnts[0]);
            Assert.That(controller.TrySetSelectedNeedsForDevelopment(
                50d, 0d, 0d, 100d, out _), Is.True);

            Assert.That(host.RebuildWorld(), Is.True);
            Assert.That(host.SpawnOrResetSelectableGridAnts(out string message),
                Is.True, message);

            Assert.That(host.SelectionController.SelectableAnts.All(ant =>
                ant.Record.Needs.Health == 100d &&
                ant.Record.Needs.Hunger == 100d &&
                ant.Record.Needs.Thirst == 100d &&
                ant.Record.Needs.Fatigue == 20d), Is.True);
        }

        [Test]
        public void Selection_SwitchesAndEmptySpaceDeselectsWithoutStoppingOrders()
        {
            AntGameDevelopmentWorldHost host = OpenHostWithSelectableAnts();
            AntSelectionCommandController controller = host.SelectionController;
            SelectableGridAnt first = controller.SelectableAnts[0];
            SelectableGridAnt second = controller.SelectableAnts[1];

            Assert.That(controller.TrySelectAtWorldPoint(first.transform.position),
                Is.True);
            Assert.That(controller.SelectedAnt, Is.SameAs(first));
            Assert.That(first.SelectionIndicator.enabled, Is.True);
            Assert.That(controller.TryIssueMoveOrder(
                SampleWorldGenerator.GridDirectDestination), Is.True);
            long firstRequest = first.Motor.PendingRequestId;

            controller.Select(second);
            Assert.That(controller.SelectedAnt, Is.SameAs(second));
            Assert.That(first.SelectionIndicator.enabled, Is.False);
            Assert.That(second.SelectionIndicator.enabled, Is.True);
            Assert.That(first.HasActiveOrder, Is.True);
            Assert.That(first.Motor.PendingRequestId, Is.EqualTo(firstRequest));

            Assert.That(controller.TrySelectAtWorldPoint(new Vector2(50f, 50f)),
                Is.False);
            Assert.That(controller.SelectedAnt, Is.Null);
            Assert.That(first.HasActiveOrder, Is.True,
                "Deselecting must not cancel another ant's movement order.");
        }

        [Test]
        public void IndependentOrders_ReplaceOnlyTheSelectedAntRequest()
        {
            AntGameDevelopmentWorldHost host = OpenHostWithSelectableAnts();
            AntSelectionCommandController controller = host.SelectionController;
            SelectableGridAnt first = controller.SelectableAnts[0];
            SelectableGridAnt second = controller.SelectableAnts[1];

            controller.Select(first);
            Assert.That(controller.TryIssueMoveOrder(
                SampleWorldGenerator.GridDirectDestination), Is.True);
            long firstRequest = first.Motor.PendingRequestId;
            Assert.That(controller.TryIssueMoveOrder(
                SampleWorldGenerator.GridDetourDestination), Is.True);
            long replacementRequest = first.Motor.PendingRequestId;
            Assert.That(replacementRequest, Is.Not.EqualTo(firstRequest));

            controller.Select(second);
            Assert.That(controller.TryIssueMoveOrder(
                SampleWorldGenerator.GridChunkBoundaryDestination), Is.True);
            Assert.That(first.Motor.PendingRequestId,
                Is.EqualTo(replacementRequest));
            Assert.That(second.Motor.PendingRequestId,
                Is.Not.EqualTo(replacementRequest));
            Assert.That(first.Destination,
                Is.EqualTo(SampleWorldGenerator.GridDetourDestination));
            Assert.That(second.Destination,
                Is.EqualTo(SampleWorldGenerator.GridChunkBoundaryDestination));
        }

        [Test]
        public void InvalidAndUiClicks_DoNotReplaceAValidOrderOrSelection()
        {
            AntGameDevelopmentWorldHost host = OpenHostWithSelectableAnts();
            AntSelectionCommandController controller = host.SelectionController;
            SelectableGridAnt first = controller.SelectableAnts[0];
            controller.Select(first);
            Assert.That(controller.TryIssueMoveOrder(
                SampleWorldGenerator.GridDirectDestination), Is.True);
            long request = first.Motor.PendingRequestId;

            Assert.That(controller.TryIssueMoveOrder(new Vector2Int(0, -1000)),
                Is.False);
            Assert.That(first.Motor.PendingRequestId, Is.EqualTo(request));
            Assert.That(first.FailureReason, Does.Contain("No traversable"));

            controller.SetDevelopmentUiVisible(true);
            Rect playerPanel = controller.PlayerPanelGuiRect;
            Vector2 guiPoint = playerPanel.center;
            Vector2 screenPoint = new(guiPoint.x, Screen.height - guiPoint.y);
            Assert.That(controller.IsScreenPointBlocked(screenPoint), Is.True);
            Assert.That(controller.TryHandleLeftClick(screenPoint), Is.False);
            Assert.That(controller.SelectedAnt, Is.SameAs(first));
            Assert.That(first.Motor.PendingRequestId, Is.EqualTo(request));
        }

        [Test]
        public void CancelSelectedOrder_ReturnsOnlyThatAntToIdle()
        {
            AntGameDevelopmentWorldHost host = OpenHostWithSelectableAnts();
            AntSelectionCommandController controller = host.SelectionController;
            SelectableGridAnt first = controller.SelectableAnts[0];
            SelectableGridAnt second = controller.SelectableAnts[1];

            controller.Select(first);
            Assert.That(controller.TryIssueMoveOrder(
                SampleWorldGenerator.GridDirectDestination), Is.True);
            controller.Select(second);
            Assert.That(controller.TryIssueMoveOrder(
                SampleWorldGenerator.GridDetourDestination), Is.True);
            long firstRequest = first.Motor.PendingRequestId;

            Assert.That(controller.CancelSelectedOrder(), Is.True);
            Assert.That(second.Motor.State, Is.EqualTo(GridNavigationMotorState.Idle));
            Assert.That(second.HasActiveOrder, Is.False);
            Assert.That(first.Motor.PendingRequestId, Is.EqualTo(firstRequest));
            Assert.That(first.HasActiveOrder, Is.True);
        }

        [Test]
        public void CameraPanAndZoom_KeepScreenSpaceSelectionAccurate()
        {
            AntGameDevelopmentWorldHost host = OpenHostWithSelectableAnts();
            AntSelectionCommandController controller = host.SelectionController;
            Camera camera = Object.FindFirstObjectByType<Camera>();
            SelectableGridAnt ant = controller.SelectableAnts[2];

            camera.orthographicSize = 10f;
            camera.transform.position = new Vector3(
                ant.transform.position.x + 2f,
                ant.transform.position.y + 1f,
                -10f);
            Vector2 screenPoint = camera.WorldToScreenPoint(ant.transform.position);

            Assert.That(controller.TryHandleLeftClick(screenPoint), Is.True);
            Assert.That(controller.SelectedAnt, Is.SameAs(ant));
            Assert.That(ant.SelectionIndicator.enabled, Is.True);
        }

        private static AntGameDevelopmentWorldHost OpenHostWithSelectableAnts()
        {
            Scene scene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Single);
            AntGameDevelopmentWorldHost host = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<
                    AntGameDevelopmentWorldHost>(true))
                .Single();
            Assert.That(host.RebuildWorld(), Is.True);
            Assert.That(host.SpawnOrResetSelectableGridAnts(out string message),
                Is.True, message);
            return host;
        }
    }
}
