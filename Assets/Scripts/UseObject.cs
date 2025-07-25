using System;
using System.Collections;
using Unity.Burst.CompilerServices;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;


public class UseObject : MonoBehaviour
{
    public enum UseType { pickup, nozzle, drive, handbrake, ignition, carLights, meshToHide, interiorLights, lightSwitch, shifter, indicator, wiper, door, trash };

    public UseType useType;
    public int itemValue;
    public GameObject car;
    public bool isActive = false;
    public bool isCarried = false;

    public GameObject meshToHide;
    public GameObject lightBulb;

    public GameObject objectToSpawn;

    public float openAngle = 90f; // How far the door opens
    public float openSpeed = 2f; // Speed of opening
    public float closeSpeed = 2f; // Speed of closing


    [HideInInspector] public String startName;

    [SerializeField] private Vector3 minRotation = Vector3.zero;
    [SerializeField] private Vector3 maxRotation = Vector3.zero;
    [SerializeField] private AudioClip useSoundClip;
    [SerializeField] private AudioClip useSoundClipClose;
    [SerializeField] private AudioClip hitSoundClip;
    [SerializeField] private float followSpeed = 10f;
    [SerializeField] private float rotationSpeed = 10f;


    private Transform player, camTarget, exitTarget, startParent, holdPos;
    private Quaternion startRotation;
    private Vector3 startPosition, lastVelocity;
    private CharacterController characterController;
    private Vector2 lastMousePosition;
    private Rigidbody rb;

    private HingeJoint hinge;
    private JointLimits originalLimits;
    private bool soundPlayed = true;

    private float forceAmount = 400f;

    private AudioSource useAudioSource;

    private bool isAutoRotateActive = false;


    private void Start()
    {
        startRotation = transform.localRotation;
        startPosition = transform.position;
        startParent = transform.parent;
        player = GameObject.Find("Player").transform;
        characterController = player.GetComponent<CharacterController>();
        startName = transform.name;
        rb = transform.GetComponent<Rigidbody>();
        useAudioSource = player.GetComponent<MouseLook>().interactSource;

        // door settings
        hinge = GetComponent<HingeJoint>();
        if (hinge != null) originalLimits = hinge.limits;
    }

    public void Use(Transform target)
    {
        // state toggle
        if (isActive) isActive = false;
        else isActive = true;

        if (useType == UseType.pickup)
        {
            holdPos = target;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            isAutoRotateActive = true;
            isCarried = true;
            player.GetComponent<MouseLook>().carriedObject = gameObject;
        }
        else if (useType == UseType.lightSwitch)
        {
            if (!isActive)
            {
                transform.localRotation = Quaternion.Euler(maxRotation);
            }
            else
            {
                transform.localRotation = Quaternion.Euler(minRotation);
            }

            if (lightBulb)
            {
                Light lightSource = lightBulb.GetComponent<Light>();

                if (!lightSource.enabled)
                {
                    lightBulb.GetComponent<MeshRenderer>().material.EnableKeyword("_EMISSION");
                    lightSource.enabled = true;
                }
                else
                {
                    lightBulb.GetComponent<MeshRenderer>().material.DisableKeyword("_EMISSION");
                    lightSource.enabled = false;
                }
            }
        }
        else if (useType == UseType.nozzle)
        {
            if (isActive)
            {
                transform.parent = startParent;
                transform.localRotation = startRotation;
            }
            else
            {
                transform.parent = Camera.main.transform;
                transform.localRotation = Quaternion.Euler(0, 0, 0);
            }
        }
        else if (useType == UseType.drive)
        {
            // engage driving mode
            if (car != null && player.GetComponent<MouseLook>() != null)
            {
                if (!player.GetComponent<MouseLook>().isDriving)
                {
                    car.GetComponent<CarController>().GetInsideCar();
                }
            }
            else Debug.Log("Either car object is missing or mouse look script is not attached.");
        }
        else if (useType == UseType.interiorLights)
        {
            car.GetComponent<CarController>().TurnInteriorLightsOn();
        }
        else if (useType == UseType.handbrake)
        {
            car.GetComponent<CarController>().PullHandbrake();
        }
        else if (useType == UseType.ignition)
        {
            car.GetComponent<CarController>().IgniteEngine();

            if (meshToHide)
            {
                if (isActive == false)
                {
                    meshToHide.GetComponent<MeshRenderer>().enabled = true;
                }
                else meshToHide.GetComponent<MeshRenderer>().enabled = false;
            }
        }
        else if (useType == UseType.carLights)
        {
            car.GetComponent<CarController>().TurnLightsOn();
        }
        else if (useType == UseType.shifter)
        {
            car.GetComponent<CarController>().ShiftToNextGear();
        }
        else if (useType == UseType.indicator)
        {
            car.GetComponent<CarController>().UseIndicatorLeft();
        }
        else if (useType == UseType.wiper)
        {
            car.GetComponent<CarController>().UseWiper();
        }
        else if (useType == UseType.meshToHide)
        {
            if (isActive == false)
            {
                meshToHide.GetComponent<MeshRenderer>().enabled = true;
            }
            else
            {
                meshToHide.GetComponent<MeshRenderer>().enabled = false;
            }
        }
        else if (useType == UseType.door)
        {
            if (isActive)
            {
                // Open the door
                JointLimits openLimits = new JointLimits();
                openLimits.min = 0;
                openLimits.max = openAngle;
                hinge.limits = openLimits;

                // Apply motor to open the door
                JointMotor motor = hinge.motor;
                motor.targetVelocity = openAngle * openSpeed;
                motor.force = 100f;
                hinge.motor = motor;
                hinge.useMotor = true;

                // play sound one time
                useAudioSource.PlayOneShot(useSoundClip);
            }
            else
            {
                // Close the door
                hinge.limits = originalLimits;

                JointMotor motor = hinge.motor;
                motor.targetVelocity = -openAngle * closeSpeed;
                motor.force = 100f;
                hinge.motor = motor;
                hinge.useMotor = true;

                soundPlayed = false;
            }
        }
        else if (useType == UseType.trash)
        {
            if (meshToHide.GetComponent<MeshRenderer>().enabled)
            {
                gameObject.SetActive(false);

                Vector3 spawnPos = new Vector3(transform.position.x, transform.position.y + 0.8f, transform.position.z);

                if (objectToSpawn) Instantiate(objectToSpawn, spawnPos, transform.rotation);

                useAudioSource.PlayOneShot(useSoundClip);
            }
        }

        if (useAudioSource != null && useSoundClip != null)
        {
            if (useType == UseType.door)
            {
                if (isActive) useAudioSource.PlayOneShot(useSoundClip);
            }
            else useAudioSource.PlayOneShot(useSoundClip);
        }
    }

    public void UseSecondary()
    {
        if (useType == UseType.shifter)
        {
            car.GetComponent<CarController>().ShiftToPreviousGear();
        }
        else if (useType == UseType.indicator)
        {
            car.GetComponent<CarController>().UseIndicatorRight();
        }
    }

    void Update()
    {
        if (useType == UseType.pickup)
        {
            if (isCarried)
            {
                // throw item
                if (Input.GetMouseButtonDown(1))
                {
                    DeactivateItem();

                    // throw away held item
                    rb.AddForce(player.forward * forceAmount, ForceMode.Force);
                }

                // rotate object with use key
                if (Input.GetButton("Rotate"))
                {
                    // Get mouse movement
                    float mouseX = Input.GetAxis("Mouse X");
                    float mouseY = Input.GetAxis("Mouse Y");

                    // Rotate the object
                    transform.Rotate(player.up, -mouseX * rotationSpeed, Space.World);
                    transform.Rotate(player.right, mouseY * rotationSpeed, Space.World);

                    isAutoRotateActive = false;
                }

                if (gameObject.GetComponent<FuelTank>()) rb.mass = gameObject.GetComponent<FuelTank>().currentLiquid + 1f;
            }
        }
        else if (useType == UseType.nozzle)
        {
            if (isActive)
            {
                if (Vector3.Distance(transform.position, startPosition) > 4f)
                {
                    isActive = false;
                    transform.parent = startParent;
                    transform.localRotation = startRotation;
                }
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, startPosition, 2f * Time.deltaTime);
            }
        }
        else if (useType == UseType.door)
        {
            if (!isActive)
            {
                if (transform.localRotation == startRotation && !soundPlayed)
                {
                    useAudioSource.PlayOneShot(useSoundClipClose);
                    soundPlayed = true;
                }
            }
        }
    }

    public void DeactivateItem()
    {
        holdPos = null;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.None;
        isActive = false;
        player.GetComponent<MouseLook>().carriedObject = null;
        isAutoRotateActive = false;
        isCarried = false;
    }

    void FixedUpdate()
    {
        if (useType == UseType.pickup)
        {
            if (isCarried)
            {
                // Get the scroll wheel input
                float scrollInput = Input.GetAxis("ScrollWheel");

                Vector3 centerOfObj = transform.GetComponent<Collider>().bounds.center;

                // make the object float towards the holding position
                if (holdPos == null) return;

                if (scrollInput != 0)
                {
                    // Calculate new Z position
                    float newZPos = holdPos.localPosition.z + (scrollInput * 2f);

                    // Clamp the value between min and max
                    newZPos = Mathf.Clamp(newZPos, 1.2f, 2.2f);

                    // Apply the new position
                    holdPos.localPosition = new Vector3(0, 0, newZPos);
                }

                if (isAutoRotateActive) rb.MoveRotation(Quaternion.Slerp(transform.rotation, Quaternion.identity, Time.fixedDeltaTime * 8f));

                // Apply force
                rb.linearVelocity = (holdPos.position - centerOfObj) * followSpeed;
            }
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (useAudioSource && isActive && hitSoundClip) useAudioSource.PlayOneShot(hitSoundClip);
    }
}