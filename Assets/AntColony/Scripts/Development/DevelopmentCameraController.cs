using UnityEngine;
using UnityEngine.InputSystem;
using AntColony.Commands;

namespace AntColony.Development
{
    /// <summary>Developer-only free camera for inspecting the sample world.</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class DevelopmentCameraController : MonoBehaviour
    {
        private const float TypicalWindowsScrollDelta = 120f;
        private const float MaximumScrollStepsPerFrame = 4f;

        [SerializeField] private float panSpeed = 24f;
        [SerializeField] private float zoomUnitsPerScrollStep = 4f;
        [SerializeField] private float zoomSmoothTime = 0.08f;
        [SerializeField] private float minimumOrthographicSize = 4f;
        [SerializeField] private float maximumOrthographicSize = 64f;
        [SerializeField] private Vector3 homePosition = new(0f, 0f, -10f);
        [SerializeField] private float homeOrthographicSize = 30f;
        [SerializeField] private bool inputEnabled = true;
        [SerializeField] private bool constrainPosition;
        [SerializeField] private Vector2 minimumPosition = new(-64f, -64f);
        [SerializeField] private Vector2 maximumPosition = new(64f, 64f);
        [SerializeField] private bool keepViewportInsideBounds;

        private Camera controlledCamera;
        private Vector2 previousMousePosition;
        private float targetOrthographicSize;
        private float zoomVelocity;
        private AntSelectionCommandController selectionController;

        public float TargetOrthographicSize => targetOrthographicSize;
        public float MinimumOrthographicSize => minimumOrthographicSize;
        public float MaximumOrthographicSize => maximumOrthographicSize;
        public bool InputEnabled => inputEnabled;
        public Vector2 MinimumPosition => minimumPosition;
        public Vector2 MaximumPosition => maximumPosition;
        public Vector3 HomePosition => homePosition;
        public float HomeOrthographicSize => homeOrthographicSize;

        private void Awake()
        {
            EnsureCameraReference();
            targetOrthographicSize = Mathf.Clamp(
                controlledCamera.orthographicSize,
                minimumOrthographicSize,
                maximumOrthographicSize);
            selectionController = FindFirstObjectByType<
                AntSelectionCommandController>();
        }

        private void OnValidate()
        {
            zoomUnitsPerScrollStep = Mathf.Max(0.1f, zoomUnitsPerScrollStep);
            zoomSmoothTime = Mathf.Max(0.01f, zoomSmoothTime);
            minimumOrthographicSize = Mathf.Max(0.1f, minimumOrthographicSize);
            maximumOrthographicSize = Mathf.Max(
                minimumOrthographicSize,
                maximumOrthographicSize);
            homeOrthographicSize = Mathf.Clamp(
                homeOrthographicSize,
                minimumOrthographicSize,
                maximumOrthographicSize);
        }

        private void Update()
        {
            if (!inputEnabled) return;
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (keyboard != null)
            {
                Vector2 direction = Vector2.zero;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                {
                    direction.x -= 1f;
                }
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                {
                    direction.x += 1f;
                }
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                {
                    direction.y -= 1f;
                }
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                {
                    direction.y += 1f;
                }

                ApplyPan(direction, Time.unscaledDeltaTime);
                if (keyboard.homeKey.wasPressedThisFrame)
                {
                    ReturnHome();
                }
            }

            if (mouse == null)
            {
                return;
            }

            Vector2 mousePosition = mouse.position.ReadValue();
            if (selectionController != null &&
                selectionController.IsScreenPointBlocked(mousePosition))
            {
                // Scrolling a panel or dragging its scrollbar must not also move
                // the world camera. Keyboard pan and Home remain available.
                previousMousePosition = mousePosition;
                AdvanceZoom(Time.unscaledDeltaTime);
                return;
            }

            float scroll = mouse.scroll.ReadValue().y;
            if (!Mathf.Approximately(scroll, 0f))
            {
                ApplyScrollZoom(scroll);
            }

            AdvanceZoom(Time.unscaledDeltaTime);

            if (mouse.middleButton.isPressed)
            {
                Vector2 delta = mousePosition - previousMousePosition;
                float worldUnitsPerPixel =
                    controlledCamera.orthographicSize * 2f / Screen.height;
                transform.position -= (Vector3)(delta * worldUnitsPerPixel);
                ClampPosition();
            }

            previousMousePosition = mousePosition;
        }

        /// <summary>
        /// Applies developer pan intent without changing the zoom target. Kept as
        /// a small method so camera input behaviour can be tested independently.
        /// </summary>
        public void ApplyPan(Vector2 direction, float unscaledDeltaTime)
        {
            EnsureCameraReference();
            if (unscaledDeltaTime <= 0f || direction.sqrMagnitude <= 0f)
            {
                return;
            }

            transform.position += (Vector3)direction.normalized *
                panSpeed * unscaledDeltaTime;
            ClampPosition();
        }

        /// <summary>
        /// Converts device-specific scroll values into stable zoom steps. Some
        /// mice report 120 per notch while other devices report values near 1.
        /// </summary>
        public void ApplyScrollZoom(float verticalScroll)
        {
            EnsureCameraReference();
            float scrollSteps = NormaliseScrollSteps(verticalScroll);
            if (Mathf.Approximately(scrollSteps, 0f))
            {
                return;
            }

            targetOrthographicSize = Mathf.Clamp(
                targetOrthographicSize - scrollSteps * zoomUnitsPerScrollStep,
                minimumOrthographicSize,
                maximumOrthographicSize);
        }

        /// <summary>Moves the orthographic camera smoothly toward its zoom target.</summary>
        public void AdvanceZoom(float unscaledDeltaTime)
        {
            EnsureCameraReference();
            if (unscaledDeltaTime <= 0f)
            {
                return;
            }

            controlledCamera.orthographicSize = Mathf.SmoothDamp(
                controlledCamera.orthographicSize,
                targetOrthographicSize,
                ref zoomVelocity,
                zoomSmoothTime,
                Mathf.Infinity,
                unscaledDeltaTime);
            ClampPosition();
        }

        public void ReturnHome()
        {
            EnsureCameraReference();
            transform.position = homePosition;
            controlledCamera.orthographicSize = homeOrthographicSize;
            targetOrthographicSize = homeOrthographicSize;
            zoomVelocity = 0f;
            ClampPosition();
        }

        public void ConfigureHome(Vector3 position, float orthographicSize)
        {
            homePosition = position;
            homeOrthographicSize = Mathf.Clamp(orthographicSize,
                minimumOrthographicSize, maximumOrthographicSize);
        }

        /// <summary>
        /// Applies a remembered view without changing its Home command. This is
        /// used when switching between Legacy and Isometric Surface presentation.
        /// </summary>
        public void SetCameraState(Vector3 position, float orthographicSize)
        {
            EnsureCameraReference();
            transform.position = position;
            controlledCamera.orthographicSize = Mathf.Clamp(orthographicSize,
                minimumOrthographicSize, maximumOrthographicSize);
            targetOrthographicSize = controlledCamera.orthographicSize;
            zoomVelocity = 0f;
            ClampPosition();
        }

        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
            previousMousePosition = Mouse.current != null
                ? Mouse.current.position.ReadValue() : Vector2.zero;
        }

        public void ConfigureBounds(Vector2 minimum, Vector2 maximum,
            bool keepWholeViewportInside = false)
        {
            EnsureCameraReference();
            minimumPosition = Vector2.Min(minimum, maximum);
            maximumPosition = Vector2.Max(minimum, maximum);
            constrainPosition = true;
            keepViewportInsideBounds = keepWholeViewportInside;
            ClampPosition();
        }

        private void ClampPosition()
        {
            if (!constrainPosition) return;
            Vector3 position = transform.position;
            float horizontalInset = keepViewportInsideBounds
                ? controlledCamera.orthographicSize * controlledCamera.aspect
                : 0f;
            float verticalInset = keepViewportInsideBounds
                ? controlledCamera.orthographicSize
                : 0f;
            position.x = ClampAxis(position.x, minimumPosition.x,
                maximumPosition.x, horizontalInset);
            position.y = ClampAxis(position.y, minimumPosition.y,
                maximumPosition.y, verticalInset);
            transform.position = position;
        }

        private static float ClampAxis(float value, float minimum, float maximum,
            float inset)
        {
            float innerMinimum = minimum + inset;
            float innerMaximum = maximum - inset;
            return innerMinimum <= innerMaximum
                ? Mathf.Clamp(value, innerMinimum, innerMaximum)
                : (minimum + maximum) * 0.5f;
        }

        /// <summary>Returns a bounded, device-independent number of scroll steps.</summary>
        public static float NormaliseScrollSteps(float verticalScroll)
        {
            if (float.IsNaN(verticalScroll) || float.IsInfinity(verticalScroll) ||
                Mathf.Approximately(verticalScroll, 0f))
            {
                return 0f;
            }

            float magnitude = Mathf.Max(
                1f,
                Mathf.Abs(verticalScroll) / TypicalWindowsScrollDelta);
            return Mathf.Sign(verticalScroll) *
                Mathf.Min(magnitude, MaximumScrollStepsPerFrame);
        }

        private void EnsureCameraReference()
        {
            if (controlledCamera == null)
            {
                controlledCamera = GetComponent<Camera>();
                targetOrthographicSize = controlledCamera.orthographicSize;
            }
        }
    }
}
