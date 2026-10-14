using System.Text;
using AntColony.Ants;
using AntColony.Commands;
using UnityEngine;
using UnityEngine.UI;

namespace AntColony.PlayerUI
{
    /// <summary>Concise, authoritative status for current Colony View work.</summary>
    public sealed class ColonyJobStatusView : MonoBehaviour
    {
        [SerializeField] private Text contentText;

        private readonly StringBuilder builder = new(512);
        private AntRegistry ants;
        private ConstructionDesignationRegistry construction;
        private ExcavationDesignationRegistry excavation;
        private float nextRefreshTime;

        public string DisplayedText => contentText != null ? contentText.text : string.Empty;

        private void Update()
        {
            if (Time.unscaledTime < nextRefreshTime) return;
            nextRefreshTime = Time.unscaledTime + 0.25f;
            Refresh();
        }

        public void Bind(AntRegistry registry,
            ConstructionDesignationRegistry constructionDesignations,
            ExcavationDesignationRegistry excavationDesignations)
        {
            ants = registry;
            construction = constructionDesignations;
            excavation = excavationDesignations;
            Refresh();
        }

        public void Refresh()
        {
            builder.Clear();
            builder.AppendLine("Colony work");
            int shown = 0;
            if (construction != null)
                foreach (ConstructionDesignation designation in construction.Designations)
                {
                    if (designation.State is ConstructionDesignationState.Completed or
                        ConstructionDesignationState.Cancelled) continue;
                    AppendJob(designation.Job,
                        designation.Recipe.Result.DisplayName,
                        $"materials {designation.Site.DeliveredQuantity}/{designation.Recipe.RequiredQuantity}");
                    if (++shown >= 5) break;
                }
            if (shown < 5 && excavation != null)
                foreach (ExcavationDesignation designation in excavation.Designations)
                {
                    if (designation.State is ExcavationDesignationState.Completed or
                        ExcavationDesignationState.Cancelled) continue;
                    AppendJob(designation.Job, $"Excavate {designation.OutputMaterial}",
                        string.Empty);
                    if (++shown >= 5) break;
                }
            if (shown == 0) builder.Append("No active designations.");
            contentText.text = builder.ToString();
        }

        public void Configure(Text content) => contentText = content;

        private void AppendJob(ColonyJob job, string readableName, string detail)
        {
            string state = job.Status == ColonyJobStatus.Available
                ? "Awaiting assignment" : job.Status.ToString();
            string owner = string.Empty;
            if (job.ReservationOwner.IsValid && ants?.TryGet(job.ReservationOwner,
                    out AntRecord record) == true)
                owner = $" · {record.DisplayName}";
            string progress = job.Progress > 0d ? $" · {job.Progress:P0}" : string.Empty;
            string reason = string.IsNullOrWhiteSpace(job.FailureReason)
                ? string.Empty : $" · {job.FailureReason}";
            builder.AppendLine($"{readableName} at {job.TargetCell}: {state}{owner}" +
                (string.IsNullOrEmpty(detail) ? string.Empty : $" · {detail}") +
                progress + reason);
        }
    }
}
