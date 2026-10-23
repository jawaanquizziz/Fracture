using UnityEngine;

namespace Valmon.Gameplay
{
    public enum CreatureState
    {
        Idle,
        Roaming,
        Alert,
        Corrupted,
        Captured
    }

    public class CreatureController : MonoBehaviour
    {
        [Header("Creature Info")]
        public string creatureName = "Bulbasaur";
        public string creatureType = "Grass/Poison";
        public int level = 5;
        public float maxHealth = 100f;
        public float currentHealth = 100f;
        public float catchDifficulty = 0.65f; // 0 to 1

        [Header("State & Behavior")]
        public CreatureState currentState = CreatureState.Idle;
        public float moveSpeed = 2.4f;
        public float runSpeed = 4.2f;
        public float roamRadius = 15.0f;
        public float detectionRadius = 8.0f;

        [Header("Corruption (Warden)")]
        public bool isCorrupted = false;
        public float corruptionDuration = 25f;
        private float corruptionTimer = 0f;
        public GameObject corruptionAuraEffect;

        // Roaming state
        private Vector3 spawnPoint;
        private Vector3 currentDestination;
        private float stateTimer = 0f;
        private float nextStateTime = 3f;
        private Transform playerTarget;
        private CharacterController controller;
        private Transform visualChild;
        private Vector3 visualChildStartLocalPos;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (controller == null)
            {
                controller = gameObject.AddComponent<CharacterController>();
                controller.radius = 0.5f;
                controller.height = 1.0f;
                controller.center = new Vector3(0, 0.5f, 0);
            }
        }

        void Start()
        {
            spawnPoint = transform.position;
            currentHealth = maxHealth;
            PickNewDestination();

            // Cache a visual child for the hop animation (the fallback primitive has none).
            if (transform.childCount > 0)
            {
                visualChild = transform.GetChild(0);
                visualChildStartLocalPos = visualChild.localPosition;
            }

            // Find player
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTarget = player.transform;
        }

        void Update()
        {
            if (currentState == CreatureState.Captured) return;

            // Handle Corruption decay
            if (isCorrupted)
            {
                corruptionTimer -= Time.deltaTime;
                if (corruptionTimer <= 0f)
                {
                    SetCorrupted(false);
                }
            }

            // Find player if lost
            if (playerTarget == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) playerTarget = player.transform;
            }

            stateTimer += Time.deltaTime;

            // Player distance check
            float distToPlayer = (playerTarget != null) ? Vector3.Distance(transform.position, playerTarget.position) : 999f;

            if (isCorrupted && distToPlayer < detectionRadius * 1.5f)
            {
                // Hostile chase or standoff
                currentState = CreatureState.Corrupted;
                LookAtTarget(playerTarget.position);
                Vector3 dir = (playerTarget.position - transform.position).normalized;
                dir.y = 0;
                controller.SimpleMove(dir * runSpeed * 0.7f);
            }
            else if (distToPlayer < detectionRadius && !isCorrupted)
            {
                currentState = CreatureState.Alert;
                LookAtTarget(playerTarget.position);
            }
            else
            {
                HandleNormalBehavior();
            }

            // Simple procedural hop/bob for walking
            if (visualChild != null && controller.velocity.sqrMagnitude > 0.1f)
            {
                float hop = Mathf.Abs(Mathf.Sin(Time.time * 8f)) * 0.08f;
                visualChild.localPosition = visualChildStartLocalPos + new Vector3(0, hop, 0);
            }
        }

        private void HandleNormalBehavior()
        {
            if (stateTimer >= nextStateTime)
            {
                stateTimer = 0f;
                if (currentState == CreatureState.Idle)
                {
                    currentState = CreatureState.Roaming;
                    PickNewDestination();
                    nextStateTime = Random.Range(3f, 7f);
                }
                else
                {
                    currentState = CreatureState.Idle;
                    nextStateTime = Random.Range(2f, 5f);
                }
            }

            if (currentState == CreatureState.Roaming)
            {
                Vector3 toDest = currentDestination - transform.position;
                toDest.y = 0;

                if (toDest.magnitude > 1.0f)
                {
                    LookAtTarget(currentDestination);
                    controller.SimpleMove(toDest.normalized * moveSpeed);
                }
                else
                {
                    currentState = CreatureState.Idle;
                }
            }
        }

        private void PickNewDestination()
        {
            Vector2 randomCircle = Random.insideUnitCircle * roamRadius;
            currentDestination = spawnPoint + new Vector3(randomCircle.x, 0, randomCircle.y);
        }

        private void LookAtTarget(Vector3 targetPos)
        {
            Vector3 lookDir = (targetPos - transform.position);
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 6f);
            }
        }

        public void SetCorrupted(bool corrupted)
        {
            isCorrupted = corrupted;
            if (corrupted)
            {
                corruptionTimer = corruptionDuration;
                currentState = CreatureState.Corrupted;
                catchDifficulty = Mathf.Clamp01(catchDifficulty + 0.3f);
                if (corruptionAuraEffect != null) corruptionAuraEffect.SetActive(true);
            }
            else
            {
                currentState = CreatureState.Idle;
                catchDifficulty = Mathf.Clamp01(catchDifficulty - 0.3f);
                if (corruptionAuraEffect != null) corruptionAuraEffect.SetActive(false);
            }
        }

        public void MarkAsCaptured()
        {
            currentState = CreatureState.Captured;
            // Shrink and disable collider
            controller.enabled = false;
            LeanTweenOrShrink();
        }

        private void LeanTweenOrShrink()
        {
            Destroy(gameObject, 1.2f);
        }
    }
}
