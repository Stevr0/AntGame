using UnityEngine;
using UnityEngine.Serialization;

namespace AntColony.Ants
{
    /// <summary>
    /// Identifies the cardinal climbable surfaces supported by the P1 motor.
    /// </summary>
    public enum AntSurfaceType
    {
        None,
        Floor,
        LeftWall,
        RightWall,
        Ceiling
    }

    /// <summary>
    /// Describes the most recent reason a corner path was rejected or cancelled.
    /// The value changes only on transition state changes, not every physics step.
    /// </summary>
    public enum AntTransitionFailureReason
    {
        None,
        DisconnectedSurfaces,
        InvalidTransitionDirection,
        PrevalidationBlocked,
        ForwardSweepBlocked,
        ForwardPoseBlocked,
        ReverseSweepBlocked,
        ReversePoseBlocked
    }

    /// <summary>
    /// Moves one ant across connected floors, walls, ceilings, and right-angle
    /// corners using Rigidbody2D physics.
    ///
    /// Controllers only provide world-space movement intent. Keeping surface
    /// selection and collision rules here lets a future autonomous controller
    /// reuse exactly the same physical motor without reading player input.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class AntSurfaceMotor : MonoBehaviour
    {
        private const int MaximumProbeHits = 8;
        private const int MaximumSurfaceCandidates = 6;
        private const int MaximumOverlapResults = 12;
        private const int TransitionClearanceSamples = 24;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float movementSpeed = 4f;
        [SerializeField, Min(0f)] private float rotationSpeed = 360f;

        [Header("Surface Detection")]
        [SerializeField] private LayerMask climbableSurfaces;
        [FormerlySerializedAs("floorProbeDistance")]
        [SerializeField, Min(0.01f)] private float surfaceProbeDistance = 0.18f;
        [SerializeField, Range(0f, 1f)] private float surfaceNormalTolerance = 0.7f;
        [SerializeField, Range(0f, 1f)] private float wallAttachInputThreshold = 0.5f;

        [Header("Surface Attachment")]
        [SerializeField, Min(0f)] private float surfaceSeparation = 0.02f;
        [SerializeField, Min(0f)] private float attachmentCorrectionStrength = 12f;
        [SerializeField, Min(0f)] private float maximumAttachmentCorrectionSpeed = 2f;

        [Header("Corner Transitions")]
        [SerializeField, Range(0f, 1f)] private float transitionInputThreshold = 0.45f;
        [SerializeField, Range(0f, 1f)] private float transitionSelectionBias = 0.1f;
        [SerializeField, Min(0.05f)] private float cornerLookAheadDistance = 0.75f;
        [SerializeField, Min(0.01f)] private float cornerProbeInset = 0.08f;
        [SerializeField, Min(0.05f)] private float maximumConnectedCornerDistance = 1.25f;
        [SerializeField, Min(0f)] private float cornerConnectionTolerance = 0.05f;
        [SerializeField, Min(0f)] private float transitionClearanceInset = 0.04f;

        [Header("Debugging")]
        [SerializeField] private bool showDebugGizmos;
        [SerializeField] private bool logTransitionEvents;

        // These arrays are reused by every physics step. None of the normal
        // traversal queries allocate managed memory as the ant moves.
        private readonly RaycastHit2D[] probeHits = new RaycastHit2D[MaximumProbeHits];
        private readonly SurfaceCandidate[] candidates = new SurfaceCandidate[MaximumSurfaceCandidates];
        private readonly Collider2D[] overlapResults = new Collider2D[MaximumOverlapResults];

        private Rigidbody2D antBody;
        private Collider2D antCollider;
        private CapsuleCollider2D capsuleCollider;
        private ContactFilter2D probeFilter;
        private Vector2 movementIntent;
        private Vector2 surfacePoint;
        private Vector2 surfaceNormal = Vector2.up;
        private Vector2 appliedMovementDirection;
        private float detachedGravityScale;
        private int candidateCount;

        private bool isTransitioning;
        private bool transitionWasRejected;
        private SurfaceCandidate transitionSource;
        private SurfaceCandidate transitionTarget;
        private Vector2 transitionStartPosition;
        private Vector2 transitionControlPoint;
        private Vector2 transitionTargetPosition;
        private Vector2 transitionEntryDirection;
        private Vector2 transitionExitDirection;
        private Vector2 rejectedTransitionPoint;
        private float transitionStartRotation;
        private float transitionTargetRotation;
        private float transitionPathLength;
        private float transitionProgress;
        private bool transitionForwardBlocked;
        private bool suppressRejectedTransitionRetry;
        private AntSurfaceType suppressedTransitionSource;
        private AntSurfaceType suppressedTransitionTarget;
        private Vector2 suppressedTransitionIntent;

        /// <summary>Gets whether a surface or a validated corner path is attached.</summary>
        public bool IsAttached => SurfaceType != AntSurfaceType.None || isTransitioning;

        /// <summary>Gets whether the current attached surface is a floor.</summary>
        public bool IsGrounded => !isTransitioning && SurfaceType == AntSurfaceType.Floor;

        /// <summary>Gets the current cardinal surface attachment type.</summary>
        public AntSurfaceType SurfaceType { get; private set; }

        /// <summary>Gets the latest attachment point selected by the motor.</summary>
        public Vector2 SurfacePoint => surfacePoint;

        /// <summary>Gets the current outward direction away from the surface.</summary>
        public Vector2 SurfaceNormal => surfaceNormal;

        /// <summary>Gets whether the motor is moving around a validated corner.</summary>
        public bool IsTransitioning => isTransitioning;

        /// <summary>Gets progress from zero at the source to one at the destination.</summary>
        public float TransitionProgress => transitionProgress;

        /// <summary>Gets the source surface of the active or most recent transition.</summary>
        public AntSurfaceType TransitionStartSurface => transitionSource.Type;

        /// <summary>Gets the destination surface of the active or most recent transition.</summary>
        public AntSurfaceType TransitionDestinationSurface => transitionTarget.Type;

        /// <summary>Gets the direction currently accepted by the physical motor.</summary>
        public Vector2 AcceptedMovementDirection => appliedMovementDirection;

        /// <summary>Gets the most recent transition rejection or cancellation reason.</summary>
        public AntTransitionFailureReason LastTransitionFailureReason { get; private set; }

        /// <summary>Gets the latest normalised world-direction request.</summary>
        public Vector2 MovementIntent => movementIntent;

        private void Awake()
        {
            antBody = GetComponent<Rigidbody2D>();
            antCollider = GetComponent<Collider2D>();
            capsuleCollider = antCollider as CapsuleCollider2D;
            detachedGravityScale = antBody.gravityScale;

            // Query every collision layer first. ProbeSurface then checks the
            // nearest obstruction against climbableSurfaces, so the ant cannot
            // attach through a closer non-climbable blocker.
            probeFilter = ContactFilter2D.noFilter;
            probeFilter.useTriggers = false;
        }

        /// <summary>
        /// Supplies the world direction requested by the active controller.
        /// Values above magnitude one are normalised so diagonals are not faster.
        /// </summary>
        public void SetMovementIntent(Vector2 intent)
        {
            movementIntent = Vector2.ClampMagnitude(intent, 1f);
        }

        private void FixedUpdate()
        {
            UpdateRejectedTransitionSuppression();

            if (isTransitioning)
            {
                UpdateCornerTransition();
                return;
            }

            CollectDirectSurfaceCandidates();
            SurfaceCandidate currentSurface = FindCurrentSurfaceCandidate();

            SurfaceCandidate outsideCorner = ProbeOutsideCorner(currentSurface);
            AddCandidate(outsideCorner);

            SurfaceCandidate selectedSurface = SelectSurface(currentSurface);
            if (selectedSurface.IsValid &&
                currentSurface.IsValid &&
                selectedSurface.Type != currentSurface.Type)
            {
                if (IsTransitionRetrySuppressed(currentSurface, selectedSurface))
                {
                    selectedSurface = currentSurface;
                }
                else if (TryBeginCornerTransition(currentSurface, selectedSurface))
                {
                    UpdateCornerTransition();
                    return;
                }

                // A geometrically tempting corner is ignored when the whole
                // collider cannot follow the path without overlapping terrain.
                selectedSurface = currentSurface;
            }

            ApplyAttachment(selectedSurface);

            if (IsAttached)
            {
                MoveAlongAttachedSurface(selectedSurface.Distance);
                AlignToSurface();
            }
            else
            {
                MoveWhileDetached();
            }
        }

        private void CollectDirectSurfaceCandidates()
        {
            candidateCount = 0;
            AddCandidate(ProbeSurface(Vector2.down, Vector2.up, AntSurfaceType.Floor));
            AddCandidate(ProbeSurface(Vector2.left, Vector2.right, AntSurfaceType.LeftWall));
            AddCandidate(ProbeSurface(Vector2.right, Vector2.left, AntSurfaceType.RightWall));
            AddCandidate(ProbeSurface(Vector2.up, Vector2.down, AntSurfaceType.Ceiling));
        }

        private SurfaceCandidate ProbeSurface(
            Vector2 direction,
            Vector2 expectedNormal,
            AntSurfaceType surfaceType)
        {
            int hitCount = antCollider.Cast(direction, probeFilter, probeHits, surfaceProbeDistance);
            RaycastHit2D nearestHit = FindNearestExternalHit(hitCount);

            if (nearestHit.collider == null || !IsClimbableLayer(nearestHit.collider.gameObject.layer))
            {
                return SurfaceCandidate.Invalid;
            }

            // Each cardinal probe accepts only the matching face. This prevents a
            // floor query from silently acquiring a nearby wall at a junction.
            if (Vector2.Dot(nearestHit.normal, expectedNormal) < surfaceNormalTolerance)
            {
                return SurfaceCandidate.Invalid;
            }

            return new SurfaceCandidate(
                surfaceType,
                nearestHit.point,
                nearestHit.normal.normalized,
                nearestHit.distance,
                false,
                nearestHit.collider);
        }

        private SurfaceCandidate ProbeOutsideCorner(SurfaceCandidate currentSurface)
        {
            if (!currentSurface.IsValid)
            {
                return SurfaceCandidate.Invalid;
            }

            Vector2 entryDirection = ProjectIntentOntoSurface(
                movementIntent,
                currentSurface.Normal);
            if (entryDirection.magnitude < transitionInputThreshold)
            {
                return SurfaceCandidate.Invalid;
            }

            entryDirection.Normalize();

            // The sample starts just beyond the current contact and slightly
            // inside the solid's outline. Casting back toward the corner exposes
            // the perpendicular face around a convex edge without moving the ant
            // or reaching across a gap.
            Vector2 probeOrigin = currentSurface.Point +
                entryDirection * cornerLookAheadDistance -
                currentSurface.Normal * cornerProbeInset;

            int hitCount = Physics2D.Raycast(
                probeOrigin,
                -entryDirection,
                probeFilter,
                probeHits,
                cornerLookAheadDistance * 2f);
            RaycastHit2D nearestHit = FindNearestExternalHit(hitCount);

            if (nearestHit.collider == null ||
                nearestHit.distance <= 0.001f ||
                !IsClimbableLayer(nearestHit.collider.gameObject.layer))
            {
                return SurfaceCandidate.Invalid;
            }

            Vector2 nextNormal = nearestHit.normal.normalized;
            if (Vector2.Dot(nextNormal, entryDirection) < surfaceNormalTolerance ||
                Mathf.Abs(Vector2.Dot(nextNormal, currentSurface.Normal)) > 1f - surfaceNormalTolerance)
            {
                return SurfaceCandidate.Invalid;
            }

            AntSurfaceType nextType = ClassifySurface(nextNormal);
            if (nextType == AntSurfaceType.None || nextType == currentSurface.Type)
            {
                return SurfaceCandidate.Invalid;
            }

            return new SurfaceCandidate(
                nextType,
                nearestHit.point,
                nextNormal,
                nearestHit.distance,
                true,
                nearestHit.collider);
        }

        private RaycastHit2D FindNearestExternalHit(int hitCount)
        {
            float nearestDistance = float.PositiveInfinity;
            RaycastHit2D nearestHit = default;

            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit2D hit = probeHits[index];
                if (hit.collider == null || hit.collider.attachedRigidbody == antBody)
                {
                    continue;
                }

                if (hit.distance < nearestDistance)
                {
                    nearestDistance = hit.distance;
                    nearestHit = hit;
                }
            }

            return nearestHit;
        }

        private void AddCandidate(SurfaceCandidate candidate)
        {
            if (!candidate.IsValid || candidateCount >= MaximumSurfaceCandidates)
            {
                return;
            }

            candidates[candidateCount] = candidate;
            candidateCount++;
        }

        private SurfaceCandidate FindCurrentSurfaceCandidate()
        {
            if (SurfaceType == AntSurfaceType.None)
            {
                return SurfaceCandidate.Invalid;
            }

            SurfaceCandidate bestMatch = SurfaceCandidate.Invalid;
            for (int index = 0; index < candidateCount; index++)
            {
                SurfaceCandidate candidate = candidates[index];
                if (candidate.Type != SurfaceType ||
                    Vector2.Dot(candidate.Normal, surfaceNormal) < surfaceNormalTolerance)
                {
                    continue;
                }

                if (!bestMatch.IsValid || candidate.Distance < bestMatch.Distance)
                {
                    bestMatch = candidate;
                }
            }

            return bestMatch;
        }

        private SurfaceCandidate SelectSurface(SurfaceCandidate currentSurface)
        {
            if (!currentSurface.IsValid)
            {
                return SelectInitialSurface();
            }

            if (movementIntent.sqrMagnitude < 0.0001f)
            {
                return currentSurface;
            }

            float currentScore = ProjectIntentOntoSurface(
                movementIntent,
                currentSurface.Normal).magnitude;
            SurfaceCandidate bestCandidate = currentSurface;
            float bestScore = currentScore;

            for (int index = 0; index < candidateCount; index++)
            {
                SurfaceCandidate candidate = candidates[index];
                if (candidate.Type == currentSurface.Type ||
                    !NormalsFormCorner(currentSurface.Normal, candidate.Normal))
                {
                    continue;
                }

                float candidateScore = ProjectIntentOntoSurface(
                    movementIntent,
                    candidate.Normal).magnitude;

                bool intentionallySelectsInsideCorner =
                    !candidate.IsOutsideCorner &&
                    candidateScore >= transitionInputThreshold &&
                    candidateScore > currentScore + transitionSelectionBias;

                bool intentionallyRoundsOutsideCorner =
                    candidate.IsOutsideCorner &&
                    currentScore >= transitionInputThreshold &&
                    candidateScore >= transitionInputThreshold;

                if ((intentionallySelectsInsideCorner || intentionallyRoundsOutsideCorner) &&
                    candidateScore >= bestScore - transitionSelectionBias)
                {
                    bestCandidate = candidate;
                    bestScore = candidateScore;
                }
            }

            return bestCandidate;
        }

        private SurfaceCandidate SelectInitialSurface()
        {
            SurfaceCandidate floor = FindCandidate(AntSurfaceType.Floor);
            if (floor.IsValid)
            {
                return floor;
            }

            // Detached ants never acquire a ceiling from open air. A nearby wall
            // may be acquired only through the established upward climb command.
            if (movementIntent.y > wallAttachInputThreshold)
            {
                SurfaceCandidate leftWall = FindCandidate(AntSurfaceType.LeftWall);
                SurfaceCandidate rightWall = FindCandidate(AntSurfaceType.RightWall);

                if (movementIntent.x < 0f && leftWall.IsValid)
                {
                    return leftWall;
                }

                if (movementIntent.x > 0f && rightWall.IsValid)
                {
                    return rightWall;
                }

                if (leftWall.IsValid && rightWall.IsValid)
                {
                    return leftWall.Distance <= rightWall.Distance ? leftWall : rightWall;
                }

                return leftWall.IsValid ? leftWall : rightWall;
            }

            return SurfaceCandidate.Invalid;
        }

        private SurfaceCandidate FindCandidate(AntSurfaceType type)
        {
            for (int index = 0; index < candidateCount; index++)
            {
                if (candidates[index].Type == type)
                {
                    return candidates[index];
                }
            }

            return SurfaceCandidate.Invalid;
        }

        private bool TryBeginCornerTransition(
            SurfaceCandidate source,
            SurfaceCandidate target)
        {
            // Store both ends before validation so a rejected plan can suppress
            // repeated attempts for this exact surface pair and unchanged input.
            transitionSource = source;
            transitionTarget = target;

            if (!TryFindCornerPivot(source, target, out Vector2 pivot))
            {
                RejectTransition(
                    target.Point,
                    AntTransitionFailureReason.DisconnectedSurfaces);
                return false;
            }

            Vector2 exitDirection = ProjectIntentOntoSurface(
                movementIntent,
                target.Normal);
            if (exitDirection.magnitude < transitionInputThreshold)
            {
                RejectTransition(
                    pivot,
                    AntTransitionFailureReason.InvalidTransitionDirection);
                return false;
            }

            exitDirection.Normalize();

            Vector2 entryDirection = ProjectIntentOntoSurface(
                movementIntent,
                source.Normal);
            if (entryDirection.sqrMagnitude < 0.0001f)
            {
                // Floor-to-wall and wall-to-ceiling choices commonly use an
                // intent perpendicular to the old surface. In that case, the
                // geometry tells us which way the ant approached the corner.
                entryDirection = ProjectIntentOntoSurface(
                    pivot - antBody.position,
                    source.Normal);
            }

            if (entryDirection.sqrMagnitude < 0.0001f)
            {
                RejectTransition(
                    pivot,
                    AntTransitionFailureReason.InvalidTransitionDirection);
                return false;
            }

            entryDirection.Normalize();

            GetCapsuleClearance(out float halfThickness, out float halfLength);
            Vector2 targetPosition = pivot +
                target.Normal * (halfThickness + surfaceSeparation) +
                exitDirection * (halfLength + surfaceSeparation);

            Vector2 freeCornerDirection = (source.Normal + target.Normal).normalized;
            Vector2 controlPoint = pivot + freeCornerDirection *
                (halfLength + halfThickness + surfaceSeparation);

            float targetRotation = Vector2.SignedAngle(Vector2.up, target.Normal);

            // Sample the complete path before committing. A shrunken copy of the
            // ant's collider must remain clear at every sample; otherwise the ant
            // safely stays on its current surface.
            for (int sample = 1; sample <= TransitionClearanceSamples; sample++)
            {
                // Include the destination pose and densely sample near the source.
                // The old sparse sampling skipped the first real physics step,
                // allowing a transition to start and then fail immediately.
                float progress = sample / (float)TransitionClearanceSamples;
                Vector2 samplePosition = EvaluateQuadraticBezier(
                    antBody.position,
                    controlPoint,
                    targetPosition,
                    progress);
                float sampleRotation = Mathf.LerpAngle(antBody.rotation, targetRotation, progress);

                if (!IsPoseClear(samplePosition, sampleRotation))
                {
                    RejectTransition(
                        samplePosition,
                        AntTransitionFailureReason.PrevalidationBlocked);
                    return false;
                }
            }

            transitionSource = source;
            transitionTarget = target;
            transitionStartPosition = antBody.position;
            transitionControlPoint = controlPoint;
            transitionTargetPosition = targetPosition;
            transitionEntryDirection = entryDirection;
            transitionExitDirection = exitDirection;
            transitionStartRotation = antBody.rotation;
            transitionTargetRotation = targetRotation;
            transitionPathLength = Mathf.Max(
                Vector2.Distance(transitionStartPosition, transitionControlPoint) +
                Vector2.Distance(transitionControlPoint, transitionTargetPosition),
                0.01f);
            transitionProgress = 0f;
            transitionForwardBlocked = false;
            suppressRejectedTransitionRetry = false;
            transitionWasRejected = false;
            LastTransitionFailureReason = AntTransitionFailureReason.None;
            isTransitioning = true;
            antBody.gravityScale = 0f;
            antBody.linearVelocity = Vector2.zero;
            antBody.angularVelocity = 0f;

            LogTransitionEvent(
                $"Started {source.Type} -> {target.Type} corner transition.",
                false);
            return true;
        }

        private void UpdateCornerTransition()
        {
            float forwardIntent = Vector2.Dot(movementIntent, transitionExitDirection);
            float reverseAlongSource = Vector2.Dot(movementIntent, -transitionEntryDirection);
            float reverseAgainstDestination = Vector2.Dot(movementIntent, -transitionExitDirection);
            float reverseIntent = Mathf.Max(reverseAlongSource, reverseAgainstDestination);
            float progressDirection = 0f;

            if (forwardIntent >= transitionInputThreshold && !transitionForwardBlocked)
            {
                progressDirection = 1f;
            }
            else if (reverseIntent >= transitionInputThreshold)
            {
                progressDirection = -1f;
            }

            antBody.gravityScale = 0f;
            antBody.linearVelocity = Vector2.zero;
            antBody.angularVelocity = 0f;

            if (progressDirection == 0f)
            {
                appliedMovementDirection = Vector2.zero;
                return;
            }

            float linearProgressStep = movementSpeed * Time.fixedDeltaTime / transitionPathLength;
            float transitionAngle = Mathf.Abs(Mathf.DeltaAngle(
                transitionStartRotation,
                transitionTargetRotation));
            float rotationalProgressStep = transitionAngle > 0.01f
                ? rotationSpeed * Time.fixedDeltaTime / transitionAngle
                : linearProgressStep;

            // Position and orientation advance together at the slower configured
            // rate. This prevents a fast positional arc from snapping rotation.
            float progressStep = Mathf.Min(linearProgressStep, rotationalProgressStep);
            float proposedProgress = Mathf.Clamp01(
                transitionProgress + progressDirection * progressStep);

            // Reaching the source is a state-machine exit, not a new movement
            // step. Evaluating overlap first used to let ordinary source contact
            // reject cancellation and leave the ant permanently transitioning.
            if (progressDirection < 0f && proposedProgress <= 0.0001f)
            {
                transitionProgress = 0f;
                FinishCornerTransition(transitionSource, false);
                return;
            }

            Vector2 proposedPosition = EvaluateQuadraticBezier(
                transitionStartPosition,
                transitionControlPoint,
                transitionTargetPosition,
                proposedProgress);
            float proposedRotation = Mathf.LerpAngle(
                transitionStartRotation,
                transitionTargetRotation,
                proposedProgress);

            bool sweepIsClear = CanSweepTo(proposedPosition);
            bool poseIsClear = sweepIsClear && IsPoseClear(proposedPosition, proposedRotation);
            if (!sweepIsClear || !poseIsClear)
            {
                AntTransitionFailureReason failureReason = progressDirection > 0f
                    ? sweepIsClear
                        ? AntTransitionFailureReason.ForwardPoseBlocked
                        : AntTransitionFailureReason.ForwardSweepBlocked
                    : sweepIsClear
                        ? AntTransitionFailureReason.ReversePoseBlocked
                        : AntTransitionFailureReason.ReverseSweepBlocked;

                RejectTransition(proposedPosition, failureReason);
                appliedMovementDirection = Vector2.zero;

                if (progressDirection > 0f)
                {
                    // Do not retry the same invalid forward step forever. At the
                    // source we can cancel immediately; after partial progress we
                    // pause and preserve the already validated reverse path.
                    transitionForwardBlocked = true;

                    if (transitionProgress <= 0.0001f)
                    {
                        FinishCornerTransition(transitionSource, false);
                    }
                }
                else
                {
                    // A changed scene could theoretically obstruct the already
                    // travelled reverse path. Exit to a real surface at the
                    // current pose when possible, otherwise detach explicitly.
                    CancelTransitionAtCurrentPosition();
                }

                return;
            }

            transitionProgress = proposedProgress;
            surfacePoint = Vector2.Lerp(
                transitionSource.Point,
                transitionTarget.Point,
                transitionProgress);
            surfaceNormal = Vector2.Lerp(
                transitionSource.Normal,
                transitionTarget.Normal,
                transitionProgress).normalized;
            appliedMovementDirection = progressDirection > 0f
                ? transitionExitDirection
                : -transitionEntryDirection;

            antBody.MovePosition(proposedPosition);
            antBody.MoveRotation(proposedRotation);

            if (Mathf.Approximately(transitionProgress, 1f))
            {
                FinishCornerTransition(transitionTarget, true);
            }
            else if (Mathf.Approximately(transitionProgress, 0f))
            {
                FinishCornerTransition(transitionSource, false);
            }
        }

        private void FinishCornerTransition(
            SurfaceCandidate completedSurface,
            bool clearFailure)
        {
            isTransitioning = false;
            transitionForwardBlocked = false;
            SurfaceType = completedSurface.Type;
            surfacePoint = completedSurface.Point;
            surfaceNormal = completedSurface.Normal;
            antBody.gravityScale = 0f;

            if (clearFailure)
            {
                transitionWasRejected = false;
                LastTransitionFailureReason = AntTransitionFailureReason.None;
            }

            LogTransitionEvent(
                $"Finished on {completedSurface.Type} at progress {transitionProgress:0.00}.",
                false);
        }

        private bool TryFindCornerPivot(
            SurfaceCandidate source,
            SurfaceCandidate target,
            out Vector2 pivot)
        {
            if (source.Collider == null || target.Collider == null)
            {
                pivot = default;
                return false;
            }

            if (source.Collider != target.Collider)
            {
                ColliderDistance2D colliderDistance = source.Collider.Distance(target.Collider);
                if (!colliderDistance.isValid || colliderDistance.distance > cornerConnectionTolerance)
                {
                    // Intersecting infinite surface lines are not enough: the
                    // actual colliders must touch, overlap, or be within the very
                    // small authored seam tolerance. This rejects nearby gaps.
                    pivot = default;
                    return false;
                }
            }

            Vector2 sourceTangent = Perpendicular(source.Normal);
            Vector2 targetTangent = Perpendicular(target.Normal);
            float denominator = Cross(sourceTangent, targetTangent);

            if (Mathf.Abs(denominator) < 0.001f)
            {
                pivot = default;
                return false;
            }

            float sourceDistance = Cross(target.Point - source.Point, targetTangent) / denominator;
            pivot = source.Point + sourceTangent * sourceDistance;

            return Vector2.Distance(pivot, source.Point) <= maximumConnectedCornerDistance &&
                Vector2.Distance(pivot, target.Point) <= maximumConnectedCornerDistance;
        }

        private bool CanSweepTo(Vector2 proposedPosition)
        {
            Vector2 displacement = proposedPosition - antBody.position;
            float distance = displacement.magnitude;
            if (distance < 0.0001f)
            {
                return true;
            }

            int hitCount = antCollider.Cast(
                displacement / distance,
                probeFilter,
                probeHits,
                distance);

            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit2D hit = probeHits[index];
                if (hit.collider == null || hit.collider.attachedRigidbody == antBody)
                {
                    continue;
                }

                // Source and destination colliders are the two surfaces the ant
                // is intentionally touching around this corner. The inset pose
                // overlap check still prevents penetrating either one; treating
                // their expected sweep contacts as blockers caused false stalls.
                if (hit.collider == transitionSource.Collider ||
                    hit.collider == transitionTarget.Collider)
                {
                    continue;
                }

                // Zero-distance contacts are the source/target surfaces already
                // supporting the ant. A positive hit along the path is a blocker.
                if (hit.distance > transitionClearanceInset && hit.distance < distance)
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsPoseClear(Vector2 position, float rotation)
        {
            int overlapCount;

            if (capsuleCollider != null)
            {
                Vector2 scale = new Vector2(
                    Mathf.Abs(capsuleCollider.transform.lossyScale.x),
                    Mathf.Abs(capsuleCollider.transform.lossyScale.y));
                Vector2 size = Vector2.Scale(capsuleCollider.size, scale);
                size.x = Mathf.Max(0.01f, size.x - transitionClearanceInset * 2f);
                size.y = Mathf.Max(0.01f, size.y - transitionClearanceInset * 2f);
                float capsuleRotation = capsuleCollider.direction == CapsuleDirection2D.Horizontal
                    ? rotation
                    : rotation + 90f;

                overlapCount = Physics2D.OverlapCapsule(
                    position,
                    size,
                    CapsuleDirection2D.Horizontal,
                    capsuleRotation,
                    probeFilter,
                    overlapResults);
            }
            else
            {
                Vector2 size = antCollider.bounds.size -
                    Vector3.one * (transitionClearanceInset * 2f);
                size.x = Mathf.Max(0.01f, size.x);
                size.y = Mathf.Max(0.01f, size.y);
                overlapCount = Physics2D.OverlapBox(
                    position,
                    size,
                    rotation,
                    probeFilter,
                    overlapResults);
            }

            for (int index = 0; index < overlapCount; index++)
            {
                Collider2D overlap = overlapResults[index];
                if (overlap != null && overlap.attachedRigidbody != antBody)
                {
                    return false;
                }
            }

            return true;
        }

        private void GetCapsuleClearance(out float halfThickness, out float halfLength)
        {
            Vector2 worldSize = antCollider.bounds.size;
            if (capsuleCollider != null)
            {
                Vector2 scale = new Vector2(
                    Mathf.Abs(capsuleCollider.transform.lossyScale.x),
                    Mathf.Abs(capsuleCollider.transform.lossyScale.y));
                worldSize = Vector2.Scale(capsuleCollider.size, scale);
            }

            halfThickness = Mathf.Min(worldSize.x, worldSize.y) * 0.5f;
            halfLength = Mathf.Max(worldSize.x, worldSize.y) * 0.5f;
        }

        private void CancelTransitionAtCurrentPosition()
        {
            isTransitioning = false;
            transitionForwardBlocked = false;
            appliedMovementDirection = Vector2.zero;
            antBody.linearVelocity = Vector2.zero;
            antBody.angularVelocity = 0f;

            CollectDirectSurfaceCandidates();
            SurfaceCandidate sourceAtCurrentPose = FindCandidate(transitionSource.Type);
            SurfaceCandidate targetAtCurrentPose = FindCandidate(transitionTarget.Type);
            SurfaceCandidate recoveredSurface = sourceAtCurrentPose.IsValid
                ? sourceAtCurrentPose
                : targetAtCurrentPose;

            if (recoveredSurface.IsValid)
            {
                ApplyAttachment(recoveredSurface);
                LogTransitionEvent(
                    $"Cancelled transition onto nearby {recoveredSurface.Type}.",
                    true);
                return;
            }

            // No supported surface exists at the last validated physical pose.
            // Detaching is safer than retaining an inescapable virtual corner.
            SurfaceType = AntSurfaceType.None;
            surfacePoint = antBody.position;
            surfaceNormal = Vector2.up;
            antBody.gravityScale = detachedGravityScale;
            LogTransitionEvent(
                "Cancelled transition as detached because no surface remained reachable.",
                true);
        }

        private void SuppressRejectedTransitionRetry()
        {
            suppressRejectedTransitionRetry = true;
            suppressedTransitionSource = transitionSource.Type;
            suppressedTransitionTarget = transitionTarget.Type;
            suppressedTransitionIntent = movementIntent;
        }

        private bool IsTransitionRetrySuppressed(
            SurfaceCandidate source,
            SurfaceCandidate target)
        {
            return suppressRejectedTransitionRetry &&
                source.Type == suppressedTransitionSource &&
                target.Type == suppressedTransitionTarget;
        }

        private void UpdateRejectedTransitionSuppression()
        {
            if (!suppressRejectedTransitionRetry)
            {
                return;
            }

            bool inputReleased = movementIntent.sqrMagnitude < 0.0001f;
            bool inputChanged = !inputReleased &&
                Vector2.Dot(
                    movementIntent.normalized,
                    suppressedTransitionIntent.normalized) < 0.95f;

            if (inputReleased || inputChanged)
            {
                suppressRejectedTransitionRetry = false;
            }
        }

        private void RejectTransition(
            Vector2 point,
            AntTransitionFailureReason reason)
        {
            transitionWasRejected = true;
            rejectedTransitionPoint = point;
            LastTransitionFailureReason = reason;
            SuppressRejectedTransitionRetry();
            LogTransitionEvent($"Transition rejected: {reason}.", true);
        }

        private void LogTransitionEvent(string message, bool warning)
        {
            if (!logTransitionEvents)
            {
                return;
            }

            if (warning)
            {
                Debug.LogWarning($"{nameof(AntSurfaceMotor)}: {message}", this);
            }
            else
            {
                Debug.Log($"{nameof(AntSurfaceMotor)}: {message}", this);
            }
        }

        private void ApplyAttachment(SurfaceCandidate selectedSurface)
        {
            if (!selectedSurface.IsValid)
            {
                SurfaceType = AntSurfaceType.None;
                surfacePoint = antBody.position;
                surfaceNormal = Vector2.up;
                antBody.gravityScale = detachedGravityScale;
                return;
            }

            SurfaceType = selectedSurface.Type;
            surfacePoint = selectedSurface.Point;
            surfaceNormal = selectedSurface.Normal;
            antBody.gravityScale = 0f;
            antBody.angularVelocity = 0f;
        }

        private void MoveAlongAttachedSurface(float distanceFromSurface)
        {
            appliedMovementDirection = ProjectIntentOntoSurface(movementIntent, surfaceNormal);
            Vector2 velocity = appliedMovementDirection * movementSpeed;

            // Correct only a positive gap. At contact distance zero, the normal
            // Physics2D solver owns collision response and the motor does not push
            // continuously into terrain.
            float gap = Mathf.Max(0f, distanceFromSurface - surfaceSeparation);
            float correctionSpeed = Mathf.Min(
                gap * attachmentCorrectionStrength,
                maximumAttachmentCorrectionSpeed);
            antBody.linearVelocity = velocity - surfaceNormal * correctionSpeed;
        }

        private void MoveWhileDetached()
        {
            appliedMovementDirection = Vector2.zero;
            antBody.angularVelocity = 0f;

            // Unsupported ants explicitly return to authored gravity. Air control
            // and recovery teleportation are intentionally outside this slice.
            antBody.linearVelocity = new Vector2(0f, antBody.linearVelocity.y);
        }

        private void AlignToSurface()
        {
            float targetRotation = Vector2.SignedAngle(Vector2.up, surfaceNormal);
            float nextRotation = Mathf.MoveTowardsAngle(
                antBody.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime);

            if (Mathf.Abs(Mathf.DeltaAngle(antBody.rotation, nextRotation)) > 0.1f)
            {
                antBody.MoveRotation(nextRotation);
            }
        }

        private static Vector2 ProjectIntentOntoSurface(Vector2 intent, Vector2 normal)
        {
            Vector2 tangent = Perpendicular(normal);
            return tangent * Vector2.Dot(intent, tangent);
        }

        private static Vector2 Perpendicular(Vector2 vector)
        {
            return new Vector2(-vector.y, vector.x);
        }

        private static float Cross(Vector2 first, Vector2 second)
        {
            return first.x * second.y - first.y * second.x;
        }

        private static bool NormalsFormCorner(Vector2 first, Vector2 second)
        {
            return Mathf.Abs(Vector2.Dot(first, second)) < 0.35f;
        }

        private static Vector2 EvaluateQuadraticBezier(
            Vector2 start,
            Vector2 control,
            Vector2 end,
            float progress)
        {
            float inverse = 1f - progress;
            return inverse * inverse * start +
                2f * inverse * progress * control +
                progress * progress * end;
        }

        private static AntSurfaceType ClassifySurface(Vector2 normal)
        {
            if (Mathf.Abs(normal.y) >= Mathf.Abs(normal.x))
            {
                return normal.y >= 0f ? AntSurfaceType.Floor : AntSurfaceType.Ceiling;
            }

            return normal.x >= 0f ? AntSurfaceType.LeftWall : AntSurfaceType.RightWall;
        }

        private bool IsClimbableLayer(int layer)
        {
            return (climbableSurfaces.value & (1 << layer)) != 0;
        }

        private void OnDisable()
        {
            movementIntent = Vector2.zero;
            SurfaceType = AntSurfaceType.None;
            isTransitioning = false;
            transitionForwardBlocked = false;
            suppressRejectedTransitionRetry = false;

            if (antBody != null)
            {
                antBody.gravityScale = detachedGravityScale;
                antBody.angularVelocity = 0f;
                antBody.linearVelocity = new Vector2(0f, antBody.linearVelocity.y);
            }
        }

        private void OnValidate()
        {
            movementSpeed = Mathf.Max(0f, movementSpeed);
            rotationSpeed = Mathf.Max(0f, rotationSpeed);
            surfaceProbeDistance = Mathf.Max(0.01f, surfaceProbeDistance);
            surfaceSeparation = Mathf.Max(0f, surfaceSeparation);
            attachmentCorrectionStrength = Mathf.Max(0f, attachmentCorrectionStrength);
            maximumAttachmentCorrectionSpeed = Mathf.Max(0f, maximumAttachmentCorrectionSpeed);
            cornerLookAheadDistance = Mathf.Max(0.05f, cornerLookAheadDistance);
            cornerProbeInset = Mathf.Max(0.01f, cornerProbeInset);
            maximumConnectedCornerDistance = Mathf.Max(0.05f, maximumConnectedCornerDistance);
            cornerConnectionTolerance = Mathf.Max(0f, cornerConnectionTolerance);
            transitionClearanceInset = Mathf.Max(0f, transitionClearanceInset);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos)
            {
                return;
            }

            Collider2D currentCollider = GetComponent<Collider2D>();
            if (currentCollider == null)
            {
                return;
            }

            Vector3 centre = currentCollider.bounds.center;
            Gizmos.color = IsAttached ? Color.green : Color.yellow;
            Gizmos.DrawLine(centre, centre + Vector3.down * surfaceProbeDistance);
            Gizmos.DrawLine(centre, centre + Vector3.left * surfaceProbeDistance);
            Gizmos.DrawLine(centre, centre + Vector3.right * surfaceProbeDistance);
            Gizmos.DrawLine(centre, centre + Vector3.up * surfaceProbeDistance);

            Gizmos.color = Color.white;
            Gizmos.DrawLine(centre, centre + (Vector3)movementIntent);

            for (int index = 0; index < candidateCount; index++)
            {
                SurfaceCandidate candidate = candidates[index];
                Gizmos.color = candidate.IsOutsideCorner ? Color.yellow : Color.blue;
                Gizmos.DrawWireSphere(candidate.Point, 0.06f);
                Gizmos.DrawLine(candidate.Point, candidate.Point + candidate.Normal * 0.35f);
            }

            if (IsAttached)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(surfacePoint, 0.05f);
                Gizmos.DrawLine(surfacePoint, surfacePoint + surfaceNormal * 0.5f);

                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(centre, centre + (Vector3)appliedMovementDirection);
            }

            if (isTransitioning)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f);
                Vector2 previous = transitionStartPosition;
                for (int segment = 1; segment <= 12; segment++)
                {
                    float progress = segment / 12f;
                    Vector2 next = EvaluateQuadraticBezier(
                        transitionStartPosition,
                        transitionControlPoint,
                        transitionTargetPosition,
                        progress);
                    Gizmos.DrawLine(previous, next);
                    previous = next;
                }
            }

            if (transitionWasRejected)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(rejectedTransitionPoint, 0.12f);
            }
        }
#endif

        private readonly struct SurfaceCandidate
        {
            public static readonly SurfaceCandidate Invalid = default;

            public SurfaceCandidate(
                AntSurfaceType type,
                Vector2 point,
                Vector2 normal,
                float distance,
                bool isOutsideCorner,
                Collider2D collider)
            {
                Type = type;
                Point = point;
                Normal = normal;
                Distance = distance;
                IsOutsideCorner = isOutsideCorner;
                Collider = collider;
                IsValid = true;
            }

            public AntSurfaceType Type { get; }
            public Vector2 Point { get; }
            public Vector2 Normal { get; }
            public float Distance { get; }
            public bool IsOutsideCorner { get; }
            public Collider2D Collider { get; }
            public bool IsValid { get; }
        }
    }
}
