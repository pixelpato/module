using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


public class MouseLook : MonoBehaviour
{
    public Transform holdPos;
    public float maxDisplayDistance = 20f;
    public float mouseSensitivity = 5f;
    public float cameraSpeed = 5f;
    public Camera playerCamera;
    public TextMeshProUGUI descriptionText;

    public Image selectionDot;
    public Sprite cursorIdle;
    public Sprite cursorActive;
    public AudioSource playerAudioSource;

    [SerializeField] private AudioClip flashLightSound;
    [SerializeField] private float minFov = 50f;
    [SerializeField] private float maxFov = 25f;

    private GameObject hitObject;
    private float xRotation = 0f;


    void Start ()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerCamera == null) playerCamera = GameObject.Find("Player/Main Camera").GetComponent<Camera>();
    }

    void Update()
    {
        // perform raycast function
        Raycast();

        if (Input.GetButtonDown("FlashLight"))
        {
            if (Camera.main.GetComponent<Light>().enabled == true) Camera.main.GetComponent<Light>().enabled = false;
            else Camera.main.GetComponent<Light>().enabled = true;

            // play flashlight sound
            if (flashLightSound) playerAudioSource.PlayOneShot(flashLightSound);
        }

        // enable zoom on right click
        if (Input.GetButton("Zoom"))
        {
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, maxFov, Time.deltaTime * 8f);
        }
        else playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, minFov, Time.deltaTime * 8f);
    }

    private void LateUpdate()
    {
        // Mouse Look
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Update camera rotation independently from player
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
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
            if (distance <= maxDisplayDistance)
            {
                // Display the name of the object in the UI text
                descriptionText.text = hitObject.name;

                UseObject script = hitObject.GetComponent<UseObject>();

                selectionDot.sprite = cursorActive;

                if (Input.GetButtonDown("Use"))
                {
                    if (script)
                    {
                        script.Use();
                    }
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