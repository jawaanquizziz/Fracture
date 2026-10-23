using UnityEngine;
using Valmon.Core;

namespace Valmon.Gameplay
{
    public class ThirdPersonCamera : MonoBehaviour
    {
        [Header("Target & Follow")]
        public Transform target;
        public Vector3 followOffset = new Vector3(0.0f, 1.6f, 0.0f);
        public float distance = 4.5f;
        public float minDistance = 1.0f;
        public float maxDistance = 10.0f;

        [Header("Mouse Orbit Sensitivity")]
        public float mouseSensitivityX = 140.0f;
        public float mouseSensitivityY = 100.0f;
        public float minPitch = -25.0f;
        public float maxPitch = 65.0f;

        [Header("Collision & Smoothness")]
        public float positionSmoothTime = 0.05f;
        public float rotationSmoothTime = 0.03f;
        public LayerMask obstacleLayers = ~0; // Collide with arena walls
        public float collisionRadius = 0.25f;

        private float currentYaw;
        private float currentPitch = 15.0f;
        private float targetDistance;
        private Vector3 currentVelocity;

        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            targetDistance = distance;
            Vector3 angles = transform.eulerAngles;
            currentYaw = angles.y;
            currentPitch = angles.x;
        }

        void Update()
        {
            // Toggle cursor lock with Escape
            if (ValmonInput.GetEscapeDown())
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }

            // Scroll zoom
            float scroll = ValmonInput.GetMouseScroll();
            if (Mathf.Abs(scroll) > 0.01f)
            {
                targetDistance -= scroll * 3.0f;
                targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
            }
        }

        void LateUpdate()
        {
            if (target == null) return;

            // Only rotate when cursor is locked
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                float mouseX = ValmonInput.GetMouseX() * mouseSensitivityX * 0.02f;
                float mouseY = ValmonInput.GetMouseY() * mouseSensitivityY * 0.02f;

                currentYaw += mouseX;
                currentPitch -= mouseY;
                currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
            }

            Quaternion targetRotation = Quaternion.Euler(currentPitch, currentYaw, 0.0f);
            Vector3 focusPoint = target.position + followOffset;

            // Prevent camera clipping into arena obstacles
            float desiredDist = targetDistance;
            Vector3 rayDirection = (targetRotation * -Vector3.forward).normalized;

            if (Physics.SphereCast(focusPoint, collisionRadius, rayDirection, out RaycastHit hit, targetDistance, obstacleLayers, QueryTriggerInteraction.Ignore))
            {
                desiredDist = Mathf.Clamp(hit.distance - 0.1f, minDistance, targetDistance);
            }

            Vector3 desiredPosition = focusPoint + rayDirection * desiredDist;

            // Apply position and rotation
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, positionSmoothTime);
            transform.rotation = targetRotation;
        }
    }
}
