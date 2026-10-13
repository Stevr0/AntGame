using System;
using AntColony.Ants;
using AntColony.Commands;
using NUnit.Framework;
using UnityEngine;

namespace AntColony.Tests.EditMode
{
    /// <summary>Plain-data and visual-binding checks for Milestone 3.1B.</summary>
    public sealed class AntIdentityTests
    {
        [Test]
        public void CreateUnique_ProducesDifferentStableIds()
        {
            AntId first = AntId.CreateUnique();
            AntId second = AntId.CreateUnique();

            Assert.That(first.IsValid, Is.True);
            Assert.That(second.IsValid, Is.True);
            Assert.That(second, Is.Not.EqualTo(first));
        }

        [Test]
        public void Registry_RejectsDuplicateIds()
        {
            ColonyId colony = new("colony-a");
            var registry = new AntRegistry(colony);
            AntId sharedId = new("ant-001");

            Assert.That(registry.TryRegister(
                CreateRecord(sharedId, colony, AntCaste.Queen), out _), Is.True);
            Assert.That(registry.TryRegister(
                CreateRecord(sharedId, colony, AntCaste.Worker), out string error),
                Is.False);
            Assert.That(error, Does.Contain("already registered"));
            Assert.That(registry.Count, Is.EqualTo(1));
        }

        [Test]
        public void Registry_RejectsInvalidColonyOwnership()
        {
            var registry = new AntRegistry(new ColonyId("colony-a"));
            AntRecord foreignAnt = CreateRecord(
                new AntId("ant-foreign"),
                new ColonyId("colony-b"),
                AntCaste.Worker);

            Assert.That(registry.TryRegister(foreignAnt, out string error), Is.False);
            Assert.That(error, Does.Contain("belongs to colony colony-b"));
        }

        [Test]
        public void QueenAndWorker_UseTheSameRecordType()
        {
            ColonyId colony = new("colony-a");
            AntRecord queen = CreateRecord(
                new AntId("queen"), colony, AntCaste.Queen);
            AntRecord worker = CreateRecord(
                new AntId("worker"), colony, AntCaste.Worker);

            Assert.That(queen, Is.TypeOf<AntRecord>());
            Assert.That(worker, Is.TypeOf<AntRecord>());
            Assert.That(queen.Caste, Is.EqualTo(AntCaste.Queen));
            Assert.That(worker.Caste, Is.EqualTo(AntCaste.Worker));
        }

        [Test]
        public void Attributes_AreIndependentAndRejectOutOfRangeValues()
        {
            var attributes = new AntAttributes(2, 7, 9);

            Assert.That(attributes.Strength, Is.EqualTo(2));
            Assert.That(attributes.Agility, Is.EqualTo(7));
            Assert.That(attributes.Endurance, Is.EqualTo(9));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new AntAttributes(0, 5, 5));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new AntAttributes(5, 11, 5));
        }

        [Test]
        public void Lifecycle_RecordsDeathWithoutDeletingTheAnt()
        {
            ColonyId colony = new("colony-a");
            var registry = new AntRegistry(colony);
            AntRecord record = CreateRecord(
                new AntId("ant-001"), colony, AntCaste.Worker);
            Assert.That(registry.TryRegister(record, out _), Is.True);

            record.MarkDeceased();

            Assert.That(record.LifecycleStatus,
                Is.EqualTo(AntLifecycleStatus.Deceased));
            Assert.That(record.CanAcceptMovementOrders, Is.False);
            Assert.That(registry.TryGet(record.Id, out AntRecord retained), Is.True);
            Assert.That(retained, Is.SameAs(record));
        }

        [Test]
        public void VisualBinding_RejectsDuplicateAndSurvivesRebinding()
        {
            ColonyId colony = new("colony-a");
            var records = new AntRegistry(colony);
            var visuals = new AntEntityBindingRegistry();
            AntRecord record = CreateRecord(
                new AntId("ant-001"), colony, AntCaste.Worker);
            Assert.That(records.TryRegister(record, out _), Is.True);

            GameObject firstObject = new("First Visual");
            AntEntityBinding first = firstObject.AddComponent<AntEntityBinding>();
            first.Configure(record.Id.Value);
            Assert.That(first.Bind(records, visuals, out _), Is.True);

            GameObject duplicateObject = new("Duplicate Visual");
            AntEntityBinding duplicate =
                duplicateObject.AddComponent<AntEntityBinding>();
            duplicate.Configure(record.Id.Value);
            Assert.That(duplicate.Bind(records, visuals, out string duplicateError),
                Is.False);
            Assert.That(duplicateError, Does.Contain("already bound"));

            UnityEngine.Object.DestroyImmediate(duplicateObject);
            UnityEngine.Object.DestroyImmediate(firstObject);
            Assert.That(records.TryGet(record.Id, out AntRecord retained), Is.True,
                "Destroying a visual must not remove authoritative data.");

            GameObject replacementObject = new("Replacement Visual");
            AntEntityBinding replacement =
                replacementObject.AddComponent<AntEntityBinding>();
            replacement.Configure(record.Id.Value);
            Assert.That(replacement.Bind(records, visuals, out _), Is.True);
            Assert.That(replacement.Record, Is.SameAs(retained));
            UnityEngine.Object.DestroyImmediate(replacementObject);
        }

        [Test]
        public void VisualBinding_DoesNotCreateMissingRecords()
        {
            var records = new AntRegistry(new ColonyId("colony-a"));
            var visuals = new AntEntityBindingRegistry();
            GameObject visualObject = new("Unregistered Visual");
            AntEntityBinding binding =
                visualObject.AddComponent<AntEntityBinding>();
            binding.Configure("missing-ant");

            Assert.That(binding.Bind(records, visuals, out string error), Is.False);
            Assert.That(error, Does.Contain("No authoritative ant record"));
            Assert.That(records.Count, Is.Zero);
            UnityEngine.Object.DestroyImmediate(visualObject);
        }

        private static AntRecord CreateRecord(
            AntId id,
            ColonyId colony,
            AntCaste caste)
        {
            return new AntRecord(
                id,
                $"Test {id}",
                colony,
                caste,
                new AntAttributes(5, 6, 7));
        }
    }
}
