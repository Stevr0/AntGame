using System.Collections;
using System.Linq;
using AntColony.Ants;
using AntColony.Commands;
using AntColony.Development;
using AntColony.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace AntColony.Tests.PlayMode
{
    public sealed class MaterialStorageHaulingPlayModeTests
    {
        [UnityTest]
        public IEnumerator DevelopmentSceneExposesStorageAndHaulingControls()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            Assert.That(host.MaterialStorage.Count, Is.EqualTo(5));
            Assert.That(host.SelectionController.AutonomousHaulingEnabled, Is.False);
            host.SelectionController.SetAutonomousHaulingEnabled(true);
            Assert.That(host.SelectionController.AutonomousHaulingEnabled, Is.True);
            Assert.That(host.SelectionController.ResetMaterialHaulingFixtures(
                out string error), Is.True, error);
        }

        [UnityTest]
        public IEnumerator AutonomousCarrierMovesLooseSoilIntoPhysicalStorage()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            AntSelectionCommandController controller = host.SelectionController;
            host.ColonyJobRegistry.RemoveWhere(_ => true);
            int dropsBefore = host.MaterialDrops.GetQuantity(ExcavatedMaterialType.Soil);
            host.MaterialStorage.TryGet(
                AntGameDevelopmentWorldHost.GeneralMaterialStorageFixture,
                out MaterialStorageSlot general);
            int storedBefore = general.Quantity;
            controller.SetAutonomousHaulingEnabled(true);
            controller.SetAutonomousColonyWorkEnabled(true);

            float timeout = Time.time + 35f;
            while (general.Quantity == storedBefore && Time.time < timeout)
                yield return null;

            Assert.That(general.Quantity, Is.GreaterThan(storedBefore));
            Assert.That(host.MaterialDrops.GetQuantity(ExcavatedMaterialType.Soil),
                Is.LessThan(dropsBefore));
            Assert.That(controller.SelectableAnts.Sum(
                ant => ant.Record.Inventory.TotalQuantity) +
                host.MaterialDrops.GetQuantity(ExcavatedMaterialType.Soil) +
                host.MaterialStorage.Slots.Where(slot =>
                    slot.ResourceTypeId == MaterialResourceCatalog.GetStableId(
                        ExcavatedMaterialType.Soil)).Sum(slot => slot.Quantity),
                Is.EqualTo(dropsBefore + 5));
        }

        [UnityTest]
        public IEnumerator CarrierInventoryCreatesOwnerOnlySelfDepositWork()
        {
            yield return LoadScene();
            AntGameDevelopmentWorldHost host = FindHost();
            AntSelectionCommandController controller = host.SelectionController;
            host.ColonyJobRegistry.RemoveWhere(_ => true);
            host.MaterialDrops.Clear();
            SelectableGridAnt carrier = controller.SelectableAnts[0];
            carrier.Record.Inventory.TryAdd(
                MaterialResourceCatalog.GetStableId(ExcavatedMaterialType.Stone),
                2, MaterialResourceCatalog.ProvisionalMaximumStack, false,
                out _, out _);
            host.MaterialStorage.TryGet(
                AntGameDevelopmentWorldHost.StoneMaterialStorageFixture,
                out MaterialStorageSlot storage);
            controller.SetAutonomousHaulingEnabled(true);
            controller.SetAutonomousColonyWorkEnabled(true);

            float timeout = Time.time + 35f;
            while (storage.Quantity == 0 && Time.time < timeout)
                yield return null;

            Assert.That(storage.Quantity, Is.EqualTo(2));
            Assert.That(carrier.Record.Inventory.TotalQuantity, Is.Zero);
            Assert.That(carrier.Record.Skills.Get(AntSkillId.Hauling).TotalExperience,
                Is.GreaterThan(0d));
        }

        private static IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync(
                "AntGameDevelopment", LoadSceneMode.Single);
            yield return null;
        }

        private static AntGameDevelopmentWorldHost FindHost() =>
            Object.FindFirstObjectByType<AntGameDevelopmentWorldHost>();
    }
}
