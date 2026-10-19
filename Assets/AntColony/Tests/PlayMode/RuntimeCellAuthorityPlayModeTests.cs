using System.Collections;
using AntColony.Commands;
using AntColony.Development;
using AntColony.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace AntColony.Tests.PlayMode
{
    public sealed class RuntimeCellAuthorityPlayModeTests
    {
        [UnityTest]
        public IEnumerator AuthoredScenePublishesBothRegionsAndViewsDoNotRepublish()
        {
            yield return SceneManager.LoadSceneAsync("AntGameDevelopment",
                LoadSceneMode.Single);
            yield return null;
            AntGameDevelopmentWorldHost host =
                Object.FindFirstObjectByType<AntGameDevelopmentWorldHost>();
            Assert.That(host, Is.Not.Null);
            Assert.That(host.RuntimeCells?.Current, Is.Not.Null);
            Assert.That(host.RuntimeCells.Current.Colony.RegionId,
                Is.EqualTo(AntRegionIds.Colony));
            Assert.That(host.RuntimeCells.Current.Surface.RegionId,
                Is.EqualTo(AntRegionIds.Surface));
            Assert.That(host.TwoViewWorld.SurfaceWorld, Is.SameAs(host.SurfaceWorld));
            Assert.That(host.ColonyResourceNodes, Is.Not.Null);
            ColonyResourceNodeRegistry nodeRegistry =
                host.ColonyResourceNodes;
            long nodeRegistryRevision = nodeRegistry.Revision;

            long publication = host.RuntimeCells.PublicationRevision;
            long sequence = host.RuntimeCells.Current.Colony.Changes.CurrentSequence;
            RegionCellId cellId = RegionCellId.Colony(AntRegionIds.Colony,
                new ColonyCell(0, -8));
            Assert.That(host.RuntimeCells.TryGetCell(cellId, out var before),
                Is.EqualTo(RuntimeCellReadResult.Success));

            host.TwoViewWorld.SwitchView(DevelopmentWorldView.Surface);
            host.TwoViewWorld.SwitchView(DevelopmentWorldView.Colony);

            Assert.That(host.RuntimeCells.PublicationRevision, Is.EqualTo(publication));
            Assert.That(host.RuntimeCells.Current.Colony.Changes.CurrentSequence,
                Is.EqualTo(sequence));
            Assert.That(host.RuntimeCells.TryGetCell(cellId, out var after),
                Is.EqualTo(RuntimeCellReadResult.Success));
            Assert.That(after.CellRevision, Is.EqualTo(before.CellRevision));
            Assert.That(host.ColonyResourceNodes, Is.SameAs(nodeRegistry));
            Assert.That(nodeRegistry.Revision, Is.EqualTo(nodeRegistryRevision));
        }

        [UnityTest]
        public IEnumerator GeneratedScenarioPublishesGenerationOnlyNodesAndRejectsDirectTerrainMutation()
        {
            yield return SceneManager.LoadSceneAsync("AntGameDevelopment",
                LoadSceneMode.Single);
            yield return null;
            AntGameDevelopmentWorldHost host =
                Object.FindFirstObjectByType<AntGameDevelopmentWorldHost>();
            long firstPublication = host.RuntimeCells.PublicationRevision;
            Assert.That(host.TryStartScenario(GeneratedWorldScenario.FoundingQueen,
                2203L, out string message), Is.True, message);
            yield return null;
            Assert.That(host.RuntimeCells.PublicationRevision,
                Is.EqualTo(firstPublication + 1));
            Assert.That(host.ColonyResourceNodes, Is.Not.Null);
            Assert.That(host.ColonyResourceNodes.Diagnostics.SoilCount,
                Is.GreaterThan(0));
            Assert.That(host.ColonyResourceNodes.Diagnostics.StoneCount,
                Is.GreaterThan(0));

            SurfaceCell nodeCell = default;
            bool found = false;
            for (int y = 0; y < host.SurfaceWorld.Height && !found; y++)
            for (int x = 0; x < host.SurfaceWorld.Width; x++)
            {
                if (host.ProceduralWorld.SurfaceCells[x, y].Node == null) continue;
                nodeCell = new SurfaceCell(x, y);
                found = true;
                break;
            }
            Assert.That(found, Is.True, "The validated generated seed needs a node.");
            Assert.That(host.RuntimeCells.TryGetCell(RegionCellId.Surface(
                AntRegionIds.Surface, nodeCell), out var surfaceSnapshot),
                Is.EqualTo(RuntimeCellReadResult.Success));
            Assert.That(surfaceSnapshot.Node.InteractionState,
                Is.EqualTo(RuntimeNodeInteractionState.GenerationOnly));
            Assert.That(surfaceSnapshot.Node.InitialQuantity, Is.Not.Null);
            Assert.That(surfaceSnapshot.Node.RemainingQuantity, Is.Not.Null);

            var worldCell = new Vector2Int(4, -8);
            RegionCellId colonyCell = RegionCellId.Colony(AntRegionIds.Colony,
                new ColonyCell(worldCell.x, worldCell.y));
            Assert.That(host.RuntimeCells.TryGetCell(colonyCell, out var before),
                Is.EqualTo(RuntimeCellReadResult.Success));
            long sequence = host.RuntimeCells.Current.Colony.Changes.CurrentSequence;
            Assert.That(host.ToggleForegroundSolid(worldCell), Is.False,
                "Generated Node terrain cannot bypass the active Node authority.");
            Assert.That(host.RuntimeCells.TryGetCell(colonyCell, out var after),
                Is.EqualTo(RuntimeCellReadResult.Success));
            Assert.That(after.CellRevision, Is.EqualTo(before.CellRevision));
            Assert.That(host.RuntimeCells.Current.Colony.Changes.CurrentSequence,
                Is.EqualTo(sequence));
        }

        [UnityTest]
        public IEnumerator GeneratedVariableNodeExtractionPublishesCompletePhysicalOutput()
        {
            yield return SceneManager.LoadSceneAsync("AntGameDevelopment",
                LoadSceneMode.Single);
            yield return null;
            AntGameDevelopmentWorldHost host =
                Object.FindFirstObjectByType<AntGameDevelopmentWorldHost>();
            Assert.That(host.TryStartScenario(GeneratedWorldScenario.FoundingQueen,
                2203L, out string message), Is.True, message);
            yield return null;

            ColonyCell typedCell = ProceduralWorldGenerator.FoundingStoneFace;
            var worldCell = new Vector2Int(typedCell.X, typedCell.Y);
            RegionCellId cellId = RegionCellId.Colony(
                AntRegionIds.Colony, typedCell);
            Assert.That(host.ColonyResourceNodes.TryGet(cellId, out var before),
                Is.EqualTo(ColonyResourceNodeReadResult.Success));
            Assert.That(host.WorldData.GetSolidMaterialId(worldCell,
                out string solidId), Is.EqualTo(WorldCellQueryResult.Available));

            Vector2Int start = new(host.ProceduralWorld.QueenSpawn.X,
                host.ProceduralWorld.QueenSpawn.Y);
            Assert.That(SurvivalInteractionOrder.TryMeasureApproach(
                host.GridNavigationMap, start, worldCell, out Vector2Int approach,
                out _, out string error), Is.True, error);
            Assert.That(host.ExcavationTransaction.TryCommit(
                "test.wv-b3c3b.generated-extraction", worldCell, solidId,
                approach, out WorldMaterialDrop output, out error), Is.True, error);
            Assert.That(output.Material, Is.EqualTo(ExcavatedMaterialType.Stone));
            Assert.That(host.MaterialDrops.GetQuantity(ExcavatedMaterialType.Stone),
                Is.EqualTo(before.RemainingQuantity));
            Assert.That(host.ColonyResourceNodes.TryGet(cellId, out var after),
                Is.EqualTo(ColonyResourceNodeReadResult.Success));
            Assert.That(after.NodeId, Is.EqualTo(before.NodeId));
            Assert.That(after.RemainingQuantity, Is.Zero);
            Assert.That(after.Revision, Is.GreaterThan(before.Revision));
            Assert.That(after.Lifecycle, Is.EqualTo(ColonyResourceNodeLifecycle.Depleted));
            Assert.That(host.WorldData.GetSolidMaterialId(worldCell, out string afterSolid),
                Is.EqualTo(WorldCellQueryResult.Available));
            Assert.That(string.IsNullOrEmpty(afterSolid), Is.True);
        }

        [UnityTest]
        public IEnumerator FixtureRestorePublishesOnePairWithoutDuplicateAnts()
        {
            yield return SceneManager.LoadSceneAsync("AntGameDevelopment",
                LoadSceneMode.Single);
            yield return null;
            AntGameDevelopmentWorldHost host =
                Object.FindFirstObjectByType<AntGameDevelopmentWorldHost>();
            RuntimeCellPublicationRegistry registry = host.RuntimeCells;
            long fixturePublication = registry.PublicationRevision;
            ColonyResourceNodeRegistry firstFixtureNodes =
                host.ColonyResourceNodes;

            Assert.That(host.TryStartScenario(GeneratedWorldScenario.FoundingQueen,
                2203L, out string generatedMessage), Is.True, generatedMessage);
            yield return null;
            ColonyResourceNodeRegistry generatedNodes =
                host.ColonyResourceNodes;
            Assert.That(firstFixtureNodes.Lifecycle,
                Is.EqualTo(ColonyResourceNodeRegistryLifecycle.ContextInvalidated));
            Assert.That(host.TryStartScenario(GeneratedWorldScenario.DevelopmentFixture,
                2203L, out string fixtureMessage), Is.True, fixtureMessage);
            yield return null;

            Assert.That(host.RuntimeCells, Is.SameAs(registry));
            Assert.That(registry.PublicationRevision,
                Is.EqualTo(fixturePublication + 2));
            Assert.That(host.ActiveScenario,
                Is.EqualTo(GeneratedWorldScenario.DevelopmentFixture));
            Assert.That(host.SelectionController.SelectableAnts.Count, Is.EqualTo(3));
            Assert.That(host.TwoViewWorld.Residencies.Count, Is.EqualTo(3));
            Assert.That(host.TwoViewWorld.SurfaceWorld, Is.SameAs(host.SurfaceWorld));
            Assert.That(registry.Current.Colony.RegionId,
                Is.EqualTo(AntRegionIds.Colony));
            Assert.That(registry.Current.Surface.RegionId,
                Is.EqualTo(AntRegionIds.Surface));
            Assert.That(generatedNodes.Lifecycle,
                Is.EqualTo(ColonyResourceNodeRegistryLifecycle.ContextInvalidated));
            Assert.That(host.ColonyResourceNodes, Is.Not.SameAs(firstFixtureNodes));
            Assert.That(host.ColonyResourceNodes, Is.Not.SameAs(generatedNodes));
            Assert.That(host.ColonyResourceNodes.Lifecycle,
                Is.EqualTo(ColonyResourceNodeRegistryLifecycle.Active));
        }
    }
}
