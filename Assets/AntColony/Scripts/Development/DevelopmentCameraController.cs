using UnityEngine;
using UnityEngine.InputSystem;

namespace AntColony.Development
{
    /// <summary>Developer-only free camera for inspecting the sample world.</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class DevelopmentCameraController : MonoBehaviour
    {
        [SerializeField] private float panSpeed = 24f;
        [SerializeField] private float zoomSpeed = 0.025f;
        [SerializeField] private float minimumOrthographicSize = 4f;
        [SerializeField] private float maximumOrthographicSize = 64f;
        [SerializeField] private Vector3 homePosition = new(0f, 0f, -10f);
        [SerializeField] private float homeOrthographicSize = 30f;

        private Camera controlledCamera;
        private Vector2 previousMousePosition;

        private void Awake()
        {
            controlledCamera = GetComponent<Camera>();
        }

        private void Update()
        {
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

                transform.position += (Vector3)direction.normalized *
                    panSpeed * Time.unscaledDeltaTime;
                if (keyboard.homeKey.wasPressedThisFrame)
                {
                    ReturnHome();
                }
            }

            if (mouse == null)
            {
                return;
            }

            float scroll = mouse.scroll.ReadValue().y;
            if (!Mathf.Approximately(scroll, 0f))
            {
                controlledCamera.orthographicSize = Mathf.Clamp(
                    controlledCamera.orthographicSize - scroll * zoomSpeed,
                    minimumOrthographicSize,
                    maximumOrthographicSize);
            }

            Vector2 mousePosition = mouse.position.ReadValue();
            if (mouse.middleButton.isPressed)
            {
                Vector2 delta = mousePosition - previousMousePosition;
                float worldUnitsPerPixel =
                    controlledCamera.orthographicSize * 2f / Screen.height;
                transform.position -= (Vector3)(delta * worldUnitsPerPixel);
            }

            previousMousePosition = mousePosition;
        }

        public void ReturnHome()
        {
            transform.position = homePosition;
            controlledCamera.orthographicSize = homeOrthographicSize;
        }
    }
}
