using UnityEngine;

namespace Valmon.Gameplay
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Speeds")]
        public float walkSpeed = 5.0f;
        public float sprintSpeed = 9.0f;
        public float rotationSmoothTime = 0.1f;
        public float speedChangeRate = 10.0f;

        [Header("Jumping & Gravity")]
        public float jumpHeight = 1.6f;
        public float gravity = -18.0f;
        public float jumpTimeout = 0.1f;
        public float fallTimeout = 0.15f;

        [Header("Ground Check")]
        public bool isGrounded = true;
        public float groundedOffset = -0.14f;
        public float groundedRadius = 0.35f;
        public LayerMask groundLayers = ~0; // All layers by default

        [Header("Camera & Visuals")]
        public Transform cameraTransform;
        public Transform visualRoot;

        // Player state
        private float currentSpeed;
        private float targetRotation = 0.0f;
        private float rotationVelocity;
        private float verticalVelocity;
        private float terminalVelocity = 53.0f;

        // Timeouts
        private float jumpTimeoutDelta;
        private float fallTimeoutDelta;

        private CharacterController controller;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        void Start()
        {
            jumpTimeoutDelta = jumpTimeout;
            fallTimeoutDelta = fallTimeout;
        }

        void Update()
        {
            GroundCheck();
            JumpAndGravity();
            Move();
        }

        private void GroundCheck()
        {
            // Spherical ground check below controller base
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - groundedOffset, transform.position.z);
            isGrounded = Physics.CheckSphere(spherePosition, groundedRadius, groundLayers, QueryTriggerInteraction.Ignore) 
                         || controller.isGrounded;
        }

        private void Move()
        {
            // Input
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveZ = Input.GetAxisRaw("Vertical");
            Vector2 inputDirection = new Vector2(moveX, moveZ).normalized;

            bool isSprinting = Input.GetKey(KeyCode.LeftShift);
            float targetSpeed = (inputDirection == Vector2.zero) ? 0.0f : (isSprinting ? sprintSpeed : walkSpeed);

            // Smooth speed change
            float currentHorizontalSpeed = new Vector3(controller.velocity.x, 0.0f, controller.velocity.z).magnitude;
            float speedOffset = 0.1f;

            if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                currentSpeed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed, Time.deltaTime * speedChangeRate);
            }
            else
            {
                currentSpeed = targetSpeed;
            }

            // Calculate movement vector relative to camera
            Vector3 targetDirection = Vector3.zero;

            if (inputDirection != Vector2.zero)
            {
                targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.y) * Mathf.Rad2Deg + 
                                 (cameraTransform != null ? cameraTransform.eulerAngles.y : 0.0f);
                
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref rotationVelocity, rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);

                targetDirection = Quaternion.Euler(0.0f, targetRotation, 0.0f) * Vector3.forward;
            }

            // Move the controller
            Vector3 motion = (targetDirection.normalized * currentSpeed + new Vector3(0.0f, verticalVelocity, 0.0f)) * Time.deltaTime;
            controller.Move(motion);

            // Subtle procedural tilt/bobbing for unrigged visual models
            if (visualRoot != null && inputDirection != Vector2.zero && isGrounded)
            {
                float bob = Mathf.Sin(Time.time * (isSprinting ? 14f : 9f)) * 0.04f;
                float tilt = Mathf.Cos(Time.time * (isSprinting ? 14f : 9f)) * 1.5f;
                visualRoot.localPosition = new Vector3(0f, bob, 0f);
                visualRoot.localRotation = Quaternion.Euler(tilt * 0.5f, 0f, -inputDirection.x * 2f);
            }
        }

        private void JumpAndGravity()
        {
            if (isGrounded)
            {
                fallTimeoutDelta = fallTimeout;

                // Stop velocity drop when grounded
                if (verticalVelocity < 0.0f)
                {
                    verticalVelocity = -2f;
                }

                // Jump
                if (Input.GetButtonDown("Jump") && jumpTimeoutDelta <= 0.0f)
                {
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                }

                if (jumpTimeoutDelta >= 0.0f)
                {
                    jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                jumpTimeoutDelta = jumpTimeout;

                if (fallTimeoutDelta >= 0.0f)
                {
                    fallTimeoutDelta -= Time.deltaTime;
                }
            }

            // Apply gravity over time
            if (verticalVelocity < terminalVelocity)
            {
                verticalVelocity += gravity * Time.deltaTime;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            Gizmos.color = isGrounded ? transparentGreen : transparentRed;
            Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - groundedOffset, transform.position.z), groundedRadius);
        }
    }
}
