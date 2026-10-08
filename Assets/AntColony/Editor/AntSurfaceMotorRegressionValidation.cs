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
