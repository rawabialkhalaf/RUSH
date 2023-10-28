using UnityEngine;

namespace Convai.Scripts.Utils
{
    /// <summary>
    ///     Class for handling player movement including walking, running, jumping, and looking around.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [DisallowMultipleComponent]
    [AddComponentMenu("Convai/Player Movement")]
    [HelpURL("https://docs.convai.com/api-docs/plugins-and-integrations/unity-plugin/scripts-overview")]
    public class ConvaiPlayerMovement : MonoBehaviour
    {
        [Tooltip("The speed at which the player walks.")] [Range(1, 10)] [SerializeField]
        private float walkingSpeed = 7.5f;

        [Tooltip("The speed at which the player runs.")] [Range(1, 10)] [SerializeField]
        private float runningSpeed = 11.5f;

        [Tooltip("The speed at which the player jumps.")] [Range(1, 10)] [SerializeField]
        private float jumpSpeed = 8.0f;

        [Tooltip("The gravity applied to the player.")] [Range(1, 10)] [SerializeField]
        private float gravity = 20.0f;

        [Tooltip("The main camera the player uses.")] [SerializeField]
        private Camera playerCamera;

        [Tooltip("Speed at which the player can look around.")] [Range(1, 10)] [SerializeField]
        private float lookSpeed = 2.0f;

        [Tooltip("Limit of upwards and downwards look angles.")] [Range(1, 90)] [SerializeField]
        private float lookXLimit = 45.0f;

        [HideInInspector] public bool canMove = true;

        private CharacterController _characterController;
        private Vector3 _moveDirection = Vector3.zero;
        private float _rotationX;

        private void Start()
        {
            SetupCharacter();
            LockCursor();
        }

        private void Update()
        {
            // If player can't move, we exit early and don't process rest of the script.
            if (!canMove) return;

            // Unlock the cursor when the ESC key is pressed
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true; // Make the cursor visible
            }

            // Re-lock the cursor when the left mouse button is pressed
            if (Input.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false; // Hide the cursor 
            }

            // Check for running state and move the player
            bool isRunning = Input.GetKey(KeyCode.LeftShift);
            MovePlayer(isRunning);

            // Handle the player and camera rotation
            RotatePlayerAndCamera();
        }

        private void SetupCharacter()
        {
            _characterController = GetComponent<CharacterController>();
        }

        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        /// <summary>
        ///     Enable jumping for the player if the jump key is hit and the player is grounded. Apply gravity if player is not
        ///     grounded.
        /// </summary>
        private void Jump()
        {
            if (Input.GetButton("Jump") && _characterController.isGrounded) _moveDirection.y = jumpSpeed;

            if (!_characterController.isGrounded) ApplyGravity();
        }

        private void ApplyGravity()
        {
            _moveDirection.y -= gravity * Time.deltaTime; // Decrease y-component of motion by gravity
        }

        /// <summary>
        ///     Handle player movements based on user inputs. Including running, and direction to move.
        /// </summary>
        private void MovePlayer(bool isRunning)
        {
            // Create direction vectors for the movement
            Vector3 forward = transform.TransformDirection(Vector3.forward); // Forward direction of the player
            Vector3 right = transform.TransformDirection(Vector3.right); // Right direction of the player

            // Set the speed based on whether the player is running or not
            float speed = isRunning ? runningSpeed : walkingSpeed;

            // Get input values for vertical (W/S) and horizontal (A/D) movement
            float curSpeedX = speed * Input.GetAxis("Vertical");
            float curSpeedY = speed * Input.GetAxis("Horizontal");

            // Set move direction based on input and speed
            _moveDirection = forward * curSpeedX + right * curSpeedY;

            // Apply player movement
            _characterController.Move(_moveDirection * Time.deltaTime);
        }

        /// <summary>
        ///     Rotate player and camera based on mouse input
        /// </summary>
        private void RotatePlayerAndCamera()
        {
            // Exit if the cursor is not locked to the viewport
            if (Cursor.lockState != CursorLockMode.Locked)
                return;

            // Apply up and down look by rotating the camera in the pitch (X) axis
            _rotationX += -Input.GetAxis("Mouse Y") * lookSpeed / 5;
            _rotationX = Mathf.Clamp(_rotationX, -lookXLimit, lookXLimit); // Clamp vertical angle to look limit
            playerCamera.transform.localRotation = Quaternion.Euler(_rotationX, 0, 0); // Apply vertical rotation

            // Apply left and right look by rotating the player on the yaw (Y) axis
            transform.rotation *=
                Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed / 5, 0); // Apply horizontal rotation
        }
    }
}