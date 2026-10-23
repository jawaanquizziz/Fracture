using UnityEngine;

namespace Valmon.Gameplay
{
    public class WardenController : MonoBehaviour
    {
        [Header("Tactical Camera Settings")]
        public float panSpeed = 20.0f;
        public float zoomSpeed = 25.0f;
        public float minHeight = 8.0f;
        public float maxHeight = 40.0f;

        [Header("Energy System")]
        public float maxEnergy = 100.0f;
        public float currentEnergy = 100.0f;
        public float energyRegenRate = 7.0f; // energy per second

        [Header("Ability Costs")]
        public float corruptCost = 30.0f;
        public float trapCost = 25.0f;
        public float surgeCost = 45.0f;

        [Header("Targeting")]
        public LayerMask targetMask = ~0;
        private Camera wardenCamera;
        private CreatureController selectedCreature;

        public static WardenController Instance { get; private set; }

        void Awake()
        {
            Instance = this;
            wardenCamera = GetComponent<Camera>();
            if (wardenCamera == null) wardenCamera = GetComponentInChildren<Camera>();
        }

        void Start()
        {
            currentEnergy = maxEnergy;
        }

        void Update()
        {
            // Energy Regeneration
            if (currentEnergy < maxEnergy)
            {
                currentEnergy = Mathf.Min(maxEnergy, currentEnergy + energyRegenRate * Time.deltaTime);
            }

            // Only update input if warden camera is active
            if (wardenCamera != null && !wardenCamera.enabled) return;

            HandleCameraMovement();
            HandleSelectionAndAbilities();
        }

        private void HandleCameraMovement()
        {
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveZ = Input.GetAxisRaw("Vertical");
            Vector3 move = new Vector3(moveX, 0, moveZ).normalized * (panSpeed * Time.deltaTime);

            transform.Translate(move, Space.World);

            // Zoom via Mouse Scroll
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                Vector3 pos = transform.position;
                pos.y -= scroll * zoomSpeed;
                pos.y = Mathf.Clamp(pos.y, minHeight, maxHeight);
                transform.position = pos;
            }
        }

        private void HandleSelectionAndAbilities()
        {
            // Click to select creature or point on arena
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = wardenCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 200f, targetMask))
                {
                    CreatureController creature = hit.collider.GetComponentInParent<CreatureController>();
                    if (creature != null)
                    {
                        selectedCreature = creature;
                        Debug.Log($"<color=#ff55ff><b>[Warden]</b> Selected creature: {creature.creatureName}</color>");
                    }
                }
            }

            // Hotkeys for abilities:
            // [1] Corrupt Creature
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                UseCorruptAbility();
            }

            // [2] Deploy Rift Trap
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                UseRiftTrapAbility();
            }

            // [3] Rift Surge
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            {
                UseRiftSurgeAbility();
            }
        }

        public bool UseCorruptAbility()
        {
            if (currentEnergy < corruptCost) return false;

            // Target either selected creature or closest creature to cursor
            CreatureController target = selectedCreature;
            if (target == null)
            {
                target = FindFirstObjectByType<CreatureController>();
            }

            if (target != null && !target.isCorrupted)
            {
                currentEnergy -= corruptCost;
                target.SetCorrupted(true);
                GameManager.Instance?.AddScore(isTamer: false, points: 100);
                Debug.Log($"<color=#ff2255><b>[Warden]</b> Corrupted {target.creatureName}! +100 Warden Pts</color>");
                return true;
            }

            return false;
        }

        public bool UseRiftTrapAbility()
        {
            if (currentEnergy < trapCost) return false;

            Ray ray = wardenCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 200f))
            {
                currentEnergy -= trapCost;
                SpawnRiftTrap(hit.point);
                GameManager.Instance?.AddScore(isTamer: false, points: 50);
                return true;
            }

            return false;
        }

        public bool UseRiftSurgeAbility()
        {
            if (currentEnergy < surgeCost) return false;

            currentEnergy -= surgeCost;
            GameManager.Instance?.AddScore(isTamer: false, points: 150);
            Debug.Log("<color=#ff2255><b>[Warden]</b> Rift Surge unleashed! +150 Warden Pts</color>");
            return true;
        }

        private void SpawnRiftTrap(Vector3 position)
        {
            GameObject trap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trap.name = "Rift_Trap";
            trap.transform.position = position + Vector3.up * 0.05f;
            trap.transform.localScale = new Vector3(3.5f, 0.1f, 3.5f);

            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = new Color(0.9f, 0.1f, 0.3f, 0.7f);
            trap.GetComponent<Renderer>().sharedMaterial = mat;

            Destroy(trap, 15f); // Lasts 15 seconds
        }
    }
}
