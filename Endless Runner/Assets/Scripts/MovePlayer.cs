using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System.Collections;

namespace TempleRun.Player
{
    [RequireComponent(typeof(CharacterController), typeof(PlayerInput))]
    public class MovePlayer : MonoBehaviour
    {
        [SerializeField] private float initialPlayerSpeed = 4f;
        [SerializeField] private float maximumPlayerSpeed = 30f;
        [SerializeField] private float playerSpeedIncreaseRate = 0.1f;
        [SerializeField] private float jumpHeight = 1.0f;
        [SerializeField] private float initialGravityValue = -9.81f;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private LayerMask turnLayer;
        [SerializeField] private Animator animator;
        [SerializeField] private AnimationClip slideAnimationClip;

        private float playerSpeed;
        private float gravity;
        private Vector3 movementDirection = Vector3.forward;
        private Vector3 playerVelocity;

        private PlayerInput playerInput;
        private InputAction turnAction;
        private InputAction jumpAction;
        private InputAction slideAction;

        private CharacterController characterController;

        private int slidingAnimationId;
        private bool sliding = false;

        [SerializeField] private UnityEvent<Vector3> turnEvent;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            playerInput = GetComponent<PlayerInput>();

            slidingAnimationId = Animator.StringToHash("Sliding");

            turnAction = playerInput.actions["Turn"];
            jumpAction = playerInput.actions["Jump"];
            slideAction = playerInput.actions["Slide"];
        }

        private void OnEnable()
        {
            turnAction.performed += PlayerTurn;
            slideAction.performed += PlayerSlide;
            jumpAction.performed += PlayerJump;
        }

        private void OnDisable()
        {
            turnAction.performed -= PlayerTurn;
            slideAction.performed -= PlayerSlide;
            jumpAction.performed -= PlayerJump;
        }

        private void Start()
        {
            gravity = initialGravityValue;
            playerSpeed = initialPlayerSpeed;
        }

        private void PlayerTurn(InputAction.CallbackContext context)
        {
            Vector3? turnPosition = CheckTurn(context.ReadValue<float>());
            if (!turnPosition.HasValue)
            {
                return;
            }
            Vector3 targetDirection = Quaternion.AngleAxis(90 * context.ReadValue<float>(), Vector3.up) * movementDirection;
            turnEvent.Invoke(targetDirection);
            Turn(context.ReadValue<float>(), turnPosition.Value);
        }

        private Vector3? CheckTurn(float turnValue)
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, .1f, turnLayer);
            if (hitColliders.Length != 0)
            {
                Tile tile = hitColliders[0].transform.parent.GetComponent<Tile>();
                TileType type = tile.type;
                if ((type == TileType.LEFT && turnValue == -1) ||
                    (type == TileType.RIGHT && turnValue == 1) ||
                    (type == TileType.SIDEWAYS))
                {
                    return tile.pivot.position;
                }
            }
            return null;
        }

        private void Turn(float turnValue, Vector3 turnPosition)
        {
            Vector3 tempPlayerPosition = new Vector3(turnPosition.x, transform.position.y, turnPosition.z);
            characterController.enabled = false;
            transform.position = tempPlayerPosition;
            characterController.enabled = true;

            Quaternion targetRotation = transform.rotation * Quaternion.Euler(0, 90 * turnValue, 0);
            transform.rotation = targetRotation;
            movementDirection = transform.forward.normalized;
        }

        private void PlayerSlide(InputAction.CallbackContext context)
        {
            if (!sliding && IsGrounded())
            {
                StartCoroutine(Slide());
            }
        }

        private IEnumerator Slide()
        {
            sliding = true;
            // Shrink the collider
            Vector3 originalControllerCenter = characterController.center;
            Vector3 newControllerCenter = originalControllerCenter;
            characterController.height /= 2;
            newControllerCenter.y -= characterController.height / 2;
            characterController.center = newControllerCenter;

            // Play the sliding animation
            animator.Play(slidingAnimationId);
            yield return new WaitForSeconds(slideAnimationClip.length);
            // Set the character controller collider back to normal after sliding
            characterController.height *= 2;
            characterController.center = originalControllerCenter;
            sliding = false;
        }

        private void PlayerJump(InputAction.CallbackContext context)
        {
            if (IsGrounded())
            {
                playerVelocity.y += Mathf.Sqrt(jumpHeight * gravity * -2f);
                characterController.Move(playerVelocity * Time.deltaTime);
            }
        }

        private void Update()
        {
            characterController.Move(transform.forward * playerSpeed * Time.deltaTime);

            if (IsGrounded() && playerVelocity.y < 0)
            {
                playerVelocity.y = 0f;
            }

            playerVelocity.y += gravity * Time.deltaTime;
            characterController.Move(playerVelocity * Time.deltaTime);
        }

        private bool IsGrounded(float length = .2f)
        {
            Vector3 raycastOriginFirst = transform.position;
            raycastOriginFirst.y -= characterController.height / 2f;
            raycastOriginFirst.y += .1f;

            Vector3 raycastOriginSecond = raycastOriginFirst;
            raycastOriginFirst -= transform.forward * .2f;
            raycastOriginSecond += transform.forward * .2f;

            //Debug.DrawLine(raycastOriginFirst, Vector3.down, Color.green);
            //Debug.DrawLine(raycastOriginSecond, Vector3.down, Color.red);

            if (Physics.Raycast(raycastOriginFirst, Vector3.down, out RaycastHit hit, length, groundLayer) ||
                Physics.Raycast(raycastOriginSecond, Vector3.down, out RaycastHit hit2, length, groundLayer))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
