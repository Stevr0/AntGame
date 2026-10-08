using System;
using System.Reflection;
using AntColony.Ants;
using UnityEditor;
using UnityEngine;

namespace AntColony.EditorTools
{
    /// <summary>
    /// Focused Editor regression checks for corner-transition state safety.
    ///
    /// The project does not yet have an assembly-definition-based NUnit test
    /// suite. This validator invokes the real motor transition method so the
    /// deadlock checks can run automatically without moving runtime scripts into
    /// a new assembly during a narrowly scoped bug fix.
    /// </summary>
    public static class AntSurfaceMotorRegressionValidation
    {
        private const BindingFlags InstancePrivate =
            BindingFlags.Instance | BindingFlags.NonPublic;

        /// <summary>
        /// Verifies release, both intuitive reversal inputs, blocked-forward
        /// cancellation, and rejected-transition retry suppression.
        /// </summary>
        [MenuItem("Ant Colony/Milestone 1/Validate Corner Deadlock Regression")]
        public static void ValidateCornerDeadlockRegression()
        {
            GameObject antObject = null;
            GameObject sourceObject = null;
            GameObject targetObject = null;
            GameObject blockerObject = null;

            try
            {
                antObject = new GameObject("Corner Regression Ant");
                Rigidbody2D body = antObject.AddComponent<Rigidbody2D>();
                body.gravityScale = 4f;
                CapsuleCollider2D antCollider = antObject.AddComponent<CapsuleCollider2D>();
                antCollider.direction = CapsuleDirection2D.Horizontal;
                antCollider.size = new Vector2(1.2f, 0.6f);
                AntSurfaceMotor motor = antObject.AddComponent<AntSurfaceMotor>();
                InvokePrivate(motor, "Awake");

                sourceObject = CreateStaticCollider("Regression Source", new Vector2(-10f, 0f));
                targetObject = CreateStaticCollider("Regression Target", new Vector2(10f, 0f));
                Collider2D sourceCollider = sourceObject.GetComponent<Collider2D>();
                Collider2D targetCollider = targetObject.GetComponent<Collider2D>();

                ConfigureFloorToWallTransition(
                    motor,
                    body,
                    sourceCollider,
                    targetCollider,
                    0f);
                motor.SetMovementIntent(Vector2.zero);
                InvokePrivate(motor, "UpdateCornerTransition");
                Require(motor.IsTransitioning, "Released input must pause an active transition.");
                Require(
                    Mathf.Approximately(motor.TransitionProgress, 0f),
                    "Released input changed transition progress.");

                ConfigureFloorToWallTransition(
                    motor,
                    body,
                    sourceCollider,
                    targetCollider,
                    0f);
                motor.SetMovementIntent(Vector2.left);
                InvokePrivate(motor, "UpdateCornerTransition");
                Require(!motor.IsTransitioning, "Opposite source direction did not cancel at progress zero.");
                Require(motor.SurfaceType == AntSurfaceType.Floor, "Source reversal did not restore the floor.");

                ConfigureFloorToWallTransition(
                    motor,
                    body,
                    sourceCollider,
                    targetCollider,
                    0f);
                motor.SetMovementIntent(Vector2.down);
                InvokePrivate(motor, "UpdateCornerTransition");
                Require(
                    !motor.IsTransitioning,
                    "Opposite destination direction did not cancel at progress zero.");
                Require(
                    motor.SurfaceType == AntSurfaceType.Floor,
                    "Destination-opposite reversal did not restore the floor.");

                ConfigureFloorToWallTransition(
                    motor,
                    body,
                    sourceCollider,
                    targetCollider,
                    0f);
                blockerObject = CreateStaticCollider(
                    "Regression Blocker",
                    new Vector2(0f, 0.4f),
                    new Vector2(0.5f, 0.1f));
                Physics2D.SyncTransforms();

                motor.SetMovementIntent(Vector2.up);
                InvokePrivate(motor, "UpdateCornerTransition");
                Require(
                    !motor.IsTransitioning,
                    "A blocked first transition step left the motor permanently active.");
                Require(
                    motor.SurfaceType == AntSurfaceType.Floor,
                    "A blocked first step did not return to the source surface.");
                Require(
                    motor.LastTransitionFailureReason != AntTransitionFailureReason.None,
                    "A blocked step did not record its failure reason.");
                Require(
                    GetPrivateField<bool>(motor, "suppressRejectedTransitionRetry"),
                    "A failed transition did not suppress an unchanged immediate retry.");

                ConfigureFloorToWallTransition(
                    motor,
                    body,
                    sourceCollider,
                    targetCollider,
                    0.5f);
                motor.SetMovementIntent(Vector2.up);
                InvokePrivate(motor, "UpdateCornerTransition");
                Require(
                    motor.IsTransitioning,
                    "A blocked later step should preserve the validated reverse route.");
                Require(
                    GetPrivateField<bool>(motor, "transitionForwardBlocked"),
                    "A blocked later step was not latched against repeated forward retries.");

                motor.SetMovementIntent(Vector2.down);
                InvokePrivate(motor, "UpdateCornerTransition");
                Require(
                    !motor.IsTransitioning,
                    "An unexpectedly blocked reverse step did not leave transition state safely.");

                motor.SetMovementIntent(Vector2.zero);
                InvokePrivate(motor, "UpdateRejectedTransitionSuppression");
                Require(
                    !GetPrivateField<bool>(motor, "suppressRejectedTransitionRetry"),
                    "Releasing input did not clear retry suppression.");

                Debug.Log(
                    "Corner deadlock regression validation passed: release pauses, both reversal " +
                    "directions escape, blocked entry cancels, blocked later steps preserve or " +
                    "safely cancel reversal, failure is recorded, and retry suppression clears " +
                    "after input release.");
            }
            finally
            {
                DestroyImmediately(blockerObject);
                DestroyImmediately(targetObject);
                DestroyImmediately(sourceObject);
                DestroyImmediately(antObject);
            }
        }

        /// <summary>
        /// Exercises the real opening detector and Step-Over state with floor,
        /// ceiling, wall, over-wide, and obstructed fixtures. These are focused
        /// physics checks; Play Mode remains necessary for movement feel.
        /// </summary>
        [MenuItem("Ant Colony/Milestone 1/Validate Auto Step-Over Regression")]
        public static void ValidateAutoStepOverRegression()
        {
            ValidateValidFloorOpeningAndStateControls();
            ValidateOpeningDetection(
                "Ceiling",
                AntSurfaceType.Ceiling,
                new Vector2(-0.65f, -0.32f),
                new Vector2(-1f, 0.25f),
                new Vector2(2f, 0.25f),
                new Vector2(2f, 0.5f),
                Vector2.down,
                Vector2.right,
                true,
                AntTransitionFailureReason.None);
            ValidateOpeningDetection(
                "Wall",
                AntSurfaceType.LeftWall,
                new Vector2(0.32f, -0.65f),
                new Vector2(-0.25f, -1f),
                new Vector2(-0.25f, 2f),
                new Vector2(0.5f, 2f),
                Vector2.right,
                Vector2.up,
                true,
                AntTransitionFailureReason.None);
            ValidateOpeningDetection(
                "Over-wide floor",
                AntSurfaceType.Floor,
                new Vector2(-0.65f, 0.32f),
                new Vector2(-1f, -0.25f),
                new Vector2(3f, -0.25f),
                new Vector2(2f, 0.5f),
                Vector2.up,
                Vector2.right,
                false,
                AntTransitionFailureReason.StepOverGapTooWide);
            ValidateBlockedFloorOpening();

            Debug.Log(
                "Auto Step-Over regression validation passed: floor, ceiling, and wall " +
                "one-tile openings start controlled crossings; release pauses and reverse " +
                "returns to support; two-tile and obstructed openings are rejected.");
        }

        private static void ValidateValidFloorOpeningAndStateControls()
        {
            GameObject antObject = null;
            GameObject sourceObject = null;
            GameObject destinationObject = null;

            try
            {
                AntSurfaceMotor motor = CreateStepOverAnt(
                    new Vector2(-0.65f, 0.32f), out antObject, out Rigidbody2D body);
                sourceObject = CreateStaticCollider(
                    "Valid Floor Source", new Vector2(-1f, -0.25f), new Vector2(2f, 0.5f));
                destinationObject = CreateStaticCollider(
                    "Valid Floor Destination", new Vector2(2f, -0.25f), new Vector2(2f, 0.5f));
                Physics2D.SyncTransforms();

                object source = CreateSurfaceCandidate(
                    AntSurfaceType.Floor,
                    new Vector2(body.position.x, 0f),
                    Vector2.up,
                    sourceObject.GetComponent<Collider2D>());
                motor.SetMovementIntent(Vector2.right);
                bool started = (bool)InvokePrivateWithResult(motor, "TryBeginStepOver", source);

                Require(started && motor.IsStepOverActive, "A one-tile floor opening did not start Step-Over.");
                Require(motor.SurfaceType == AntSurfaceType.None, "An active opening retained a normal surface type.");
                Require(body.gravityScale == 0f, "Controlled opening traversal did not suppress freefall.");
                Require(
                    motor.DetectedGapDistance <= motor.MaximumStepOverDistance + 0.026f,
                    "The accepted floor opening exceeded the configured one-tile capability.");

                motor.SetMovementIntent(Vector2.zero);
                float pausedProgress = motor.StepOverProgress;
                InvokePrivate(motor, "UpdateStepOver");
                Require(
                    Mathf.Approximately(motor.StepOverProgress, pausedProgress),
                    "Releasing input did not pause Step-Over.");

                motor.SetMovementIntent(Vector2.right);
                InvokePrivate(motor, "UpdateStepOver");
                Require(motor.StepOverProgress > 0f, "Forward input did not advance Step-Over.");

                motor.SetMovementIntent(Vector2.left);
                InvokePrivate(motor, "UpdateStepOver");
                Require(!motor.IsStepOverActive, "Reverse input did not return to the source support.");
                Require(motor.SurfaceType == AntSurfaceType.Floor, "Reverse did not restore floor attachment.");
            }
            finally
            {
                DestroyImmediately(destinationObject);
                DestroyImmediately(sourceObject);
                DestroyImmediately(antObject);
            }
        }

        private static void ValidateOpeningDetection(
            string label,
            AntSurfaceType surfaceType,
            Vector2 antPosition,
            Vector2 sourcePosition,
            Vector2 destinationPosition,
            Vector2 colliderSize,
            Vector2 normal,
            Vector2 intent,
            bool expectedToStart,
            AntTransitionFailureReason expectedFailure)
        {
            GameObject antObject = null;
            GameObject sourceObject = null;
            GameObject destinationObject = null;

            try
            {
                AntSurfaceMotor motor = CreateStepOverAnt(antPosition, out antObject, out Rigidbody2D body);
                if (surfaceType == AntSurfaceType.Ceiling)
                {
                    body.rotation = 180f;
                }
                else if (surfaceType == AntSurfaceType.LeftWall)
                {
                    body.rotation = -90f;
                }

                sourceObject = CreateStaticCollider(label + " Source", sourcePosition, colliderSize);
                destinationObject = CreateStaticCollider(label + " Destination", destinationPosition, colliderSize);
                Physics2D.SyncTransforms();

                object source = CreateSurfaceCandidate(
                    surfaceType,
                    body.position - normal * 0.32f,
                    normal,
                    sourceObject.GetComponent<Collider2D>());
                motor.SetMovementIntent(intent);
                bool started = (bool)InvokePrivateWithResult(motor, "TryBeginStepOver", source);

                Require(started == expectedToStart, $"{label} opening returned an unexpected start result.");
                Require(motor.IsStepOverActive == expectedToStart, $"{label} opening left an unexpected state.");
                Require(
                    motor.LastTransitionFailureReason == expectedFailure,
                    $"{label} opening reported {motor.LastTransitionFailureReason} instead of {expectedFailure}.");

                if (!expectedToStart)
                {
                    Require(
                        GetPrivateField<bool>(motor, "suppressRejectedStepOverRetry"),
                        $"{label} rejection did not suppress an unchanged retry.");
                    motor.SetMovementIntent(Vector2.zero);
                    InvokePrivate(motor, "UpdateRejectedStepOverSuppression");
                    Require(
                        !GetPrivateField<bool>(motor, "suppressRejectedStepOverRetry"),
                        $"{label} retry suppression did not clear after release.");
                }
            }
            finally
            {
                DestroyImmediately(destinationObject);
                DestroyImmediately(sourceObject);
                DestroyImmediately(antObject);
            }
        }

        private static void ValidateBlockedFloorOpening()
        {
            GameObject antObject = null;
            GameObject sourceObject = null;
            GameObject destinationObject = null;
            GameObject blockerObject = null;

            try
            {
                AntSurfaceMotor motor = CreateStepOverAnt(
                    new Vector2(-0.65f, 0.32f), out antObject, out Rigidbody2D body);
                sourceObject = CreateStaticCollider(
                    "Blocked Floor Source", new Vector2(-1f, -0.25f), new Vector2(2f, 0.5f));
                destinationObject = CreateStaticCollider(
                    "Blocked Floor Destination", new Vector2(2f, -0.25f), new Vector2(2f, 0.5f));
                blockerObject = CreateStaticCollider(
                    "Blocked Floor Obstacle", new Vector2(0.55f, 0.32f), new Vector2(0.2f, 0.5f));
                Physics2D.SyncTransforms();

                object source = CreateSurfaceCandidate(
                    AntSurfaceType.Floor,
                    new Vector2(body.position.x, 0f),
                    Vector2.up,
                    sourceObject.GetComponent<Collider2D>());
                motor.SetMovementIntent(Vector2.right);
                bool started = (bool)InvokePrivateWithResult(motor, "TryBeginStepOver", source);

                Require(!started && !motor.IsStepOverActive, "An obstructed opening started Step-Over.");
                Require(
                    motor.LastTransitionFailureReason == AntTransitionFailureReason.StepOverPathBlocked,
                    "An obstructed opening did not report StepOverPathBlocked.");
            }
            finally
            {
                DestroyImmediately(blockerObject);
                DestroyImmediately(destinationObject);
                DestroyImmediately(sourceObject);
                DestroyImmediately(antObject);
            }
        }

        private static AntSurfaceMotor CreateStepOverAnt(
            Vector2 position,
            out GameObject antObject,
            out Rigidbody2D body)
        {
            antObject = new GameObject("Step-Over Regression Ant");
            body = antObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 4f;
            CapsuleCollider2D antCollider = antObject.AddComponent<CapsuleCollider2D>();
            antCollider.direction = CapsuleDirection2D.Horizontal;
            antCollider.size = new Vector2(1.2f, 0.6f);
            AntSurfaceMotor motor = antObject.AddComponent<AntSurfaceMotor>();
            InvokePrivate(motor, "Awake");
            SetPrivateField(motor, "climbableSurfaces", (LayerMask)(1 << 0));
            body.position = position;
            body.rotation = 0f;
            Physics2D.SyncTransforms();
            return motor;
        }

        private static void ConfigureFloorToWallTransition(
            AntSurfaceMotor motor,
            Rigidbody2D body,
            Collider2D sourceCollider,
            Collider2D targetCollider,
            float progress)
        {
            body.position = Vector2.zero;
            body.rotation = 0f;
            body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();

            object source = CreateSurfaceCandidate(
                AntSurfaceType.Floor,
                new Vector2(0f, -0.3f),
                Vector2.up,
                sourceCollider);
            object target = CreateSurfaceCandidate(
                AntSurfaceType.RightWall,
                new Vector2(0.3f, 0f),
                Vector2.left,
                targetCollider);

            SetPrivateField(motor, "transitionSource", source);
            SetPrivateField(motor, "transitionTarget", target);
            SetPrivateField(motor, "transitionStartPosition", Vector2.zero);
            SetPrivateField(motor, "transitionControlPoint", new Vector2(0f, 0.5f));
            SetPrivateField(motor, "transitionTargetPosition", new Vector2(0f, 1f));
            SetPrivateField(motor, "transitionEntryDirection", Vector2.right);
            SetPrivateField(motor, "transitionExitDirection", Vector2.up);
            SetPrivateField(motor, "transitionStartRotation", 0f);
            SetPrivateField(motor, "transitionTargetRotation", 90f);
            SetPrivateField(motor, "transitionPathLength", 0.2f);
            SetPrivateField(motor, "transitionProgress", progress);
            SetPrivateField(motor, "transitionForwardBlocked", false);
            SetPrivateField(motor, "isTransitioning", true);
            SetPrivateField(motor, "suppressRejectedTransitionRetry", false);

            PropertyInfo surfaceType = typeof(AntSurfaceMotor).GetProperty(
                nameof(AntSurfaceMotor.SurfaceType),
                BindingFlags.Instance | BindingFlags.Public);
            surfaceType.SetValue(motor, AntSurfaceType.Floor);
        }

        private static object CreateSurfaceCandidate(
            AntSurfaceType type,
            Vector2 point,
            Vector2 normal,
            Collider2D collider)
        {
            Type candidateType = typeof(AntSurfaceMotor).GetNestedType(
                "SurfaceCandidate",
                BindingFlags.NonPublic);
            return Activator.CreateInstance(
                candidateType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new object[] { type, point, normal, 0f, false, collider },
                null);
        }

        private static GameObject CreateStaticCollider(
            string name,
            Vector2 position,
            Vector2? size = null)
        {
            GameObject colliderObject = new GameObject(name);
            colliderObject.transform.position = position;
            BoxCollider2D collider = colliderObject.AddComponent<BoxCollider2D>();
            collider.size = size ?? Vector2.one;
            return colliderObject;
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, InstancePrivate);
            if (method == null)
            {
                throw new InvalidOperationException($"Could not find private method '{methodName}'.");
            }

            method.Invoke(target, null);
        }

        private static object InvokePrivateWithResult(
            object target,
            string methodName,
            params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, InstancePrivate);
            if (method == null)
            {
                throw new InvalidOperationException($"Could not find private method '{methodName}'.");
            }

            return method.Invoke(target, arguments);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, InstancePrivate);
            if (field == null)
            {
                throw new InvalidOperationException($"Could not find private field '{fieldName}'.");
            }

            field.SetValue(target, value);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, InstancePrivate);
            if (field == null)
            {
                throw new InvalidOperationException($"Could not find private field '{fieldName}'.");
            }

            return (T)field.GetValue(target);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void DestroyImmediately(GameObject gameObject)
        {
            if (gameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }
    }
}
