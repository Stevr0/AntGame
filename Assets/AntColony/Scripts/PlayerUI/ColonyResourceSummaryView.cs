using AntColony.Ants;
using AntColony.Commands;
using AntColony.World;
using UnityEngine;
using UnityEngine.UI;

namespace AntColony.PlayerUI
{
    /// <summary>Bounded-refresh presentation of physical colony resources.</summary>
    public sealed class ColonyResourceSummaryView : MonoBehaviour
    {
        [SerializeField] private Text contentText;

        private readonly ColonyResourceSummary summary = new();
        private AntRegistry ants;
        private MaterialStorageWorld storage;
        private WorldMaterialDropRegistry drops;
        private ConstructionDesignationRegistry construction;
        private float nextRefreshTime;

        public ColonyResourceSummary Summary => summary;
        public string DisplayedText => contentText != null ? contentText.text : string.Empty;

        private void Update()
        {
            if (Time.unscaledTime < nextRefreshTime) return;
            nextRefreshTime = Time.unscaledTime + 0.25f;
            Refresh();
        }

        public void Bind(AntRegistry registry, MaterialStorageWorld materialStorage,
            WorldMaterialDropRegistry materialDrops,
            ConstructionDesignationRegistry constructionDesignations)
        {
            ants = registry;
            storage = materialStorage;
            drops = materialDrops;
            construction = constructionDesignations;
            Refresh();
        }

        public void Refresh()
        {
            summary.Rebuild(ants, storage, drops, construction);
            string soilId = MaterialResourceCatalog.GetStableId(ExcavatedMaterialType.Soil);
            string stoneId = MaterialResourceCatalog.GetStableId(ExcavatedMaterialType.Stone);
            contentText.text = "Physical colony resources\n" +
                Format("Soil", summary.Get(soilId)) + "\n" +
                Format("Stone", summary.Get(stoneId));
        }

        public void Configure(Text content) => contentText = content;

        private static string Format(string name, ColonyResourceAmount amount) =>
            $"{name}: stored {amount.Stored} · carried {amount.Carried} · " +
            $"loose {amount.Loose} · staged {amount.Staged}";
    }
}
