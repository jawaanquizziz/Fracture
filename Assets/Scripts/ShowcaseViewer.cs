using UnityEngine;

namespace Valmon
{
    /// <summary>
    /// Interactive turntable camera and model inspector for viewing 3D models.
    /// Controls:
    /// - Left Click & Drag: Orbit around showcase
    /// - Right Click & Drag: Pan
    /// - Scroll Wheel: Zoom in / out
    /// - Spacebar: Toggle auto-turntable rotation
    /// </summary>
    public class ShowcaseViewer : MonoBehaviour
    {
        [Header("Target & Positioning")]
        public Transform target;
        public Vector3 targetOffset = new Vector3(0, 1.2f, 0.5f);
        public float distance = 7.5f;
        public float minDistance = 1.5f;
        public float maxDistance = 25f;

        [Header("Orbit Speed")]
        public float xSpeed = 120.0f;
        public float ySpeed = 80.0f;
        public float yMinLimit = -5f;
        public float yMaxLimit = 75f;

        [Header("Auto Rotation (Turntable)")]
        public bool autoRotate = true;
        public float autoRotateSpeed = 15f;

        private float currentX = 0.0f;
        private float currentY = 15.0f;
        private float targetDistance;

        void Start()
        {
            Vector3 angles = transform.eulerAngles;
            currentX = angles.y;
            currentY = angles.x;
            targetDistance = distance;

            if (target == null)
            {
                GameObject centerPoint = new GameObject("Showcase_Center_Target");
                centerPoint.transform.position = targetOffset;
                target = centerPoint.transform;
            }
        }

        void Update()
        {
            // Toggle auto-rotation with Space
            if (Input.GetKeyDown(KeyCode.Space))
            {
                autoRotate = !autoRotate;
            }

            // Zoom via Mouse Scroll
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                targetDistance -= scroll * 5f;
                targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
            }
        }

        void LateUpdate()
        {
            if (target == null) return;

            // Manual rotation with left mouse drag
            if (Input.GetMouseButton(0))
            {
                currentX += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
                currentY -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;
                currentY = Mathf.Clamp(currentY, yMinLimit, yMaxLimit);
            }
            else if (autoRotate)
            {
                currentX += autoRotateSpeed * Time.deltaTime;
            }

            distance = Mathf.Lerp(distance, targetDistance, Time.deltaTime * 10f);

            Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
            Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
            Vector3 position = rotation * negDistance + target.position;

            transform.rotation = rotation;
            transform.position = position;
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(20, 20, 360, 160), GUI.skin.box);
            GUILayout.Label("<b>Valmon 3D Model Showcase</b>", GUI.skin.label);
            GUILayout.Label("• Left Click + Drag: Orbit Camera");
            GUILayout.Label("• Mouse Scroll Wheel: Zoom In / Out");
            GUILayout.Label($"• [Space]: Toggle Auto-Rotate (Current: {(autoRotate ? "ON" : "OFF")})");
            GUILayout.Label("• Pokemon: Bulbasaur, Ivysaur, Venusaur, Charizard, Poké Ball");
            GUILayout.Label("• Valorant: Clove, Gekko, Reyna, Tejo");
            GUILayout.EndArea();
        }
    }
}
