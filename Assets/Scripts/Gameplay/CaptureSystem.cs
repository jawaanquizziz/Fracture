using System.Collections;
using UnityEngine;
using Valmon.Core;

namespace Valmon.Gameplay
{
    public class CaptureSystem : MonoBehaviour
    {
        [Header("Poké Ball Settings")]
        public GameObject pokeBallPrefab;
        public Transform throwOrigin;
        public float throwForce = 18f;
        public float throwUpwardForce = 4.5f;
        public float throwCooldown = 1.5f;

        [Header("Capture Timing Challenge")]
        public float catchRange = 12f;
        public LayerMask creatureLayer = ~0;

        private float lastThrowTime = -10f;
        private Camera mainCamera;

        public static CaptureSystem Instance { get; private set; }

        void Awake()
        {
            Instance = this;
            mainCamera = Camera.main;
        }

        void Update()
        {
            if (mainCamera == null) mainCamera = Camera.main;

            // Throw Poké Ball on [E] or Right Click
            if (ValmonInput.GetThrowPokeBallDown() && Time.time - lastThrowTime >= throwCooldown)
            {
                ThrowPokeBall();
            }
        }

        public void ThrowPokeBall()
        {
            lastThrowTime = Time.time;

            Vector3 spawnPos = (throwOrigin != null) ? throwOrigin.position : transform.position + transform.forward * 0.8f + Vector3.up * 1.2f;
            Vector3 throwDirection = (mainCamera != null) ? mainCamera.transform.forward : transform.forward;
            throwDirection.y = Mathf.Clamp(throwDirection.y, -0.3f, 0.6f);

            GameObject ball = null;
            if (pokeBallPrefab == null)
            {
                pokeBallPrefab = Resources.Load<GameObject>("Characters/poke_ball");
            }

            if (pokeBallPrefab != null)
            {
                ball = Instantiate(pokeBallPrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                // Fallback sphere if prefab unassigned
                ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                ball.transform.localScale = Vector3.one * 0.35f;
                ball.GetComponent<Renderer>().material.color = Color.red;
                ball.transform.position = spawnPos;
            }

            ball.name = "Thrown_PokeBall";

            // Physics setup
            Rigidbody rb = ball.GetComponent<Rigidbody>();
            if (rb == null) rb = ball.AddComponent<Rigidbody>();
            rb.mass = 0.5f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            SphereCollider col = ball.GetComponent<SphereCollider>();
            if (col == null) col = ball.AddComponent<SphereCollider>();
            col.radius = 0.25f;

            // Add throw impulse
            Vector3 forceVector = throwDirection.normalized * throwForce + Vector3.up * throwUpwardForce;
            rb.AddForce(forceVector, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 15f, ForceMode.Impulse);

            // Attach CaptureProjectile handler
            CaptureProjectile projectile = ball.AddComponent<CaptureProjectile>();
            projectile.Initialize(this);
        }

        public void HandleBallHit(CreatureController creature, Vector3 hitPoint)
        {
            if (creature == null || creature.currentState == CreatureState.Captured) return;

            // Calculate Catch Probability
            float baseChance = 1.0f - creature.catchDifficulty;
            if (creature.isCorrupted) baseChance *= 0.5f; // Harder if corrupted by Warden!

            float roll = Random.value;
            bool success = roll <= baseChance;

            if (success)
            {
                int points = creature.isCorrupted ? 250 : 150;
                GameManager.Instance?.AddScore(isTamer: true, points: points);
                creature.MarkAsCaptured();
                Debug.Log($"<color=#55ff99><b>[Valmon]</b> Captured {creature.creatureName}! +{points} pts</color>");
            }
            else
            {
                Debug.Log($"<color=#ff5555><b>[Valmon]</b> {creature.creatureName} broke free!</color>");
                creature.SetCorrupted(true); // Becoming agitated
            }
        }
    }

    public class CaptureProjectile : MonoBehaviour
    {
        private CaptureSystem system;
        private bool hasHit = false;

        public void Initialize(CaptureSystem captureSystem)
        {
            system = captureSystem;
            Destroy(gameObject, 6f); // Self destruct after 6 sec
        }

        void OnCollisionEnter(Collision collision)
        {
            if (hasHit) return;

            CreatureController creature = collision.gameObject.GetComponentInParent<CreatureController>();
            if (creature != null)
            {
                hasHit = true;
                system?.HandleBallHit(creature, collision.contacts[0].point);
                Destroy(gameObject, 0.4f);
            }
        }
    }
}
