using System.Collections;
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
        }

        [UnityTest]
        public IEnumerator GeneratedScenarioPublishesGenerationOnlyNodesAndTracksTerrain()
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
            Assert.That(host.ToggleForegroundSolid(worldCell), Is.True);
            Assert.That(host.RuntimeCells.TryGetCell(colonyCell, out var after),
                Is.EqualTo(RuntimeCellReadResult.Success));
            Assert.That(after.CellRevision, Is.GreaterThan(before.CellRevision));
            Assert.That(host.RuntimeCells.Current.Colony.Changes.CurrentSequence,
                Is.EqualTo(sequence + 1));
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

            Assert.That(host.TryStartScenario(GeneratedWorldScenario.FoundingQueen,
                2203L, out string generatedMessage), Is.True, generatedMessage);
            yield return null;
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
        }
    }
}
