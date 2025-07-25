
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


public class MouseLook : MonoBehaviour
{
    public Transform holdPos;
    public float maxDisplayDistance = 20f;
    public float mouseSensitivity = 5f;
    public float cameraSpeed = 5f;
    public float moveSpeed = 10f;
    public float gravity = -9.81f;
    public int currentMoney = 0;
    public Camera playerCamera;
    public Text descriptionText;
    public Text moneyText;

    public Image selectionDot;
    public Sprite cursorIdle;
    public Sprite cursorActive;
    public LayerMask groundLayer;
    public GameObject carriedObject = null;
    public bool isDriving = false;
    public bool isCarrying = false;
    public bool isMoving = false;
    public bool isGrounded = false;
    public AudioSource interactSource;

    [SerializeField] private AudioClip flashLightSound;

    [System.Serializable]
    public class SurfaceSound
    {
        public PhysicsMaterial physicsMaterial;
        public AudioClip[] footstepSounds;
        [Range(0, 1)] public float volume = 0.8f;
    }

    [SerializeField] private float minFov = 50f;
    [SerializeField] private float maxFov = 25f;
    [SerializeField] private float footstepInterval = 0.5f;
    [SerializeField] private float minFootstepSpeed = 0.1f;
    [SerializeField] private AudioSource footstepSource;
    [SerializeField] private SurfaceSound[] surfaceSounds;

    private Vector3 playerVelocity;
    private float sprintSpeed = 1f;
    private GameObject hitObject;
    private float xRotation = 0f;
    private CharacterController charController;
    private float footstepTimer;
    private PhysicsMaterial currentMaterial;
    private Vector3 lastPosition;
    private float actualSpeed;


    void Start ()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        charController = GetComponent<CharacterController>();

        if (playerCamera == null) playerCamera = GameObject.Find("Player/Main Camera").GetComponent<Camera>();
    }

    void Update()
    {
        // perform raycast function
        Raycast();

        // check for sprinting button
        if (Input.GetButton("Sprint"))
        {
            sprintSpeed = 2.5f;
            footstepInterval = 0.3f;
        }
        else
        {
            sprintSpeed = 1f;
            footstepInterval = 0.5f;
        }

        if (Input.GetButtonDown("FlashLight"))
        {
            if (Camera.main.GetComponent<Light>().enabled == true) Camera.main.GetComponent<Light>().enabled = false;
            else Camera.main.GetComponent<Light>().enabled = true;

            // play flashlight sound
            if (flashLightSound) interactSource.PlayOneShot(flashLightSound);
        }

        // enable zoom on right click
        if (Input.GetButton("Zoom"))
        {
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, maxFov, Time.deltaTime * 8f);
        }
        else playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, minFov, Time.deltaTime * 8f);

        // Calculate actual movement between frames
            actualSpeed = (transform.position - lastPosition).magnitude / Time.deltaTime;
        lastPosition = transform.position;

        // Check if player is moving
        isMoving = actualSpeed > minFootstepSpeed;

        if (isMoving)
        {
            footstepTimer -= Time.deltaTime;

            if (footstepTimer <= 0)
            {
                PlayFootstepSound();
                footstepTimer = footstepInterval;
            }
        }
        else
        {
            footstepTimer = 0f;
        }
    }
    
    private void PlayFootstepSound()
    {
        // Raycast down to detect surface material
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, 2f))
        {
            currentMaterial = hit.collider.sharedMaterial;

            // Find matching surface sound
            foreach (var surface in surfaceSounds)
            {
                if (surface.physicsMaterial == currentMaterial && surface.footstepSounds.Length > 0)
                {
                    AudioClip clip = surface.footstepSounds[Random.Range(0, surface.footstepSounds.Length)];
                    footstepSource.PlayOneShot(clip, surface.volume);
                    return;
                }
            }
        }

        // Default sound if no material nor terrain matched
        if (surfaceSounds.Length > 0 && surfaceSounds[0].footstepSounds.Length > 0)
        {
            AudioClip clip = surfaceSounds[0].footstepSounds[Random.Range(0, surfaceSounds[0].footstepSounds.Length)];
            footstepSource.PlayOneShot(clip, surfaceSounds[0].volume);
        }
    }

    private void LateUpdate()
    {
        // Mouse Look
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Update camera rotation independently from player
        if (!(carriedObject && (Input.GetButton("Rotate"))))
        {
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            transform.Rotate(Vector3.up * mouseX);
        }

        // player movement and direction control
        if (!isDriving)
        {
            // Movement
            if (charController.isGrounded && playerVelocity.y < 0)
            {
                playerVelocity.y = 0f;
            }

            // Get input
            Vector3 move = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

            // Transform the movement direction based on camera's direction
            move = playerCamera.transform.forward * move.z + playerCamera.transform.right * move.x;
            move.y = 0f; // Keep movement on the horizontal plane

            // Move the player
            charController.Move(move * Time.deltaTime * (moveSpeed * sprintSpeed));
            playerVelocity.y += gravity * Time.deltaTime;
            charController.Move(playerVelocity * Time.deltaTime);
        }

        // safety net for falling through terrain
        isGrounded = charController.isGrounded;

        // Extra safety: Force ground snap if falling through
        if (!isGrounded)
        {
            if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, 2f, groundLayer))
            {
                charController.Move(Vector3.down * hit.distance);
                isGrounded = true;
                playerVelocity.y = 0f;
            }
        }
    }

    private void Raycast ()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            // Check if the raycast hit an object with a collider
            hitObject = hit.collider.gameObject;

            // Calculate the distance between the camera and the hit object
            float distance = Vector3.Distance(transform.position, hitObject.transform.position);

            // Check if the distance is below the maximum display distance
            if (distance <= maxDisplayDistance && (hitObject.GetComponent<UseObject>()))
            {
                // Display the name of the object in the UI text
                descriptionText.text = hitObject.name;

                UseObject script = hitObject.GetComponent<UseObject>();

                selectionDot.sprite = cursorActive;

                if (Input.GetButtonDown("Use"))
                {
                    if (script)
                    {
                        if (!carriedObject)
                        {
                            script.Use(holdPos);
                            
                        }
                        else
                        {
                            script.DeactivateItem();
  
                        }
                    }
                }
                    else if (Input.GetMouseButtonDown(1))
                    {
                        if (script) script.UseSecondary();
                    }
            }
            else
            {
                // If the object is too far, clear the UI text
                descriptionText.text = "";

                selectionDot.sprite = cursorIdle;
            }
        }
        else
        {
            // If the raycast didn't hit anything, clear the UI text
            descriptionText.text = "";

            selectionDot.sprite = cursorIdle;
        }
    }
}