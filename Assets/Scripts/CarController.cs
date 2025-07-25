using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System;
using Unity.Mathematics;
using System.Collections;
using TMPro;

public class CarController : MonoBehaviour
{
    public Transform player, driveInteractionBox;
    private GameObject indicatorLights;

    // Car Parts
    [SerializeField] private Transform monitorText;
    [SerializeField] private Transform exitTarget, camTarget, leftDoor;
    [SerializeField] private GameObject [] lightGroup;
    [SerializeField] private Light interiorLight;
    [SerializeField] private Light indicatorFL, indicatorFR, indicatorRL, indicatorRR;
    [SerializeField] private Transform headlights;
    [SerializeField] private Transform indicatorIndicator, lightIndicator, handBrakeIndicator, oilIndicator;
    [SerializeField] private GameObject gasTank;
    [SerializeField] private Transform steeringWheel, shifter, indicatorLever, pedalBreak, pedalClutch, pedalGas, rpmNeedle, speedNeedle, handBrake, fuelNeedle, wiper, wiperLever, motor;
    [SerializeField] private WheelCollider wheelColliderFL, wheelColliderFR, wheelColliderRL, wheelColliderRR;
    [SerializeField] private Transform wheelTransformFL, wheelTransformFR, wheelTransformRL, wheelTransformRR;
    [SerializeField] private TMP_Text shifterText;

    // Settings
    [SerializeField] private bool engineActive = false;
    [SerializeField] private bool isInteriorLightOn = false;
    [SerializeField] private float breakForce, maxSteerAngle;
    [SerializeField] private int currentGear = 0;
    [SerializeField] private int differentialRatio;
    [SerializeField] private float gasConsumption = 0.0001f;

    // Engine
    [SerializeField] private float motorForce;
    [SerializeField] private float [] gearRatios;
    [SerializeField] private AnimationCurve torqueCurve;
    [SerializeField] private float minRPM;
    [SerializeField] private float maxRPM;
    [SerializeField] private float motorInertia = 2f;

    [SerializeField] private float wiggleAmount = 2f;
    [SerializeField] private float wiggleSpeed = 32f;
    [SerializeField] private float wiperSpeed = 64f;

    // Sounds
    [SerializeField] private AudioSource motorSoundSource;
    [SerializeField] private AudioClip motorStartClip;
    [SerializeField] private AudioClip motorIdleClip;
    [SerializeField] private AudioClip motorStopClip;
    [SerializeField] private AudioSource interiorSoundSource;
    [SerializeField] private AudioClip handbrakeOnClip;
    [SerializeField] private AudioClip handbrakeOffClip;
    [SerializeField] private AudioClip lightSwitchClip;
    [SerializeField] private AudioClip shifterUpClip;
    [SerializeField] private AudioClip shifterDownClip;
    [SerializeField] private AudioClip leverPullClip;
    [SerializeField] private AudioClip insertKeyClip;
    [SerializeField] private AudioSource indicatorSoundSource;
    [SerializeField] private AudioClip indicatorLoopClip;

    [SerializeField] private AudioSource wiperSoundSource;
    [SerializeField] private AudioClip wiperLoopClip;

    [SerializeField] private AudioSource breakSoundSource;
    [SerializeField] private AudioClip breakClip;

    // Variables
    private float horizontalInput, gasInput;
    private float currentSteerAngle, currentTorque;
    private float breakingPower;
    //private float clutchPower;
    private float motorTorque;
    private float speed;
    private float motorRPM = 0f;
    private float wheelRPM = 0f;
    private Vector3 [] clutchAngles = new Vector3 [] { new Vector3(8, 0, 8), new Vector3(0, 0, 0), new Vector3(8, 0, 0), new Vector3(-8, 0, 0), new Vector3(8, 0, -8), new Vector3(-8, 0, -8)};
    private String [] gearNames = {"R", "N", "1", "2", "3", "4"};
    private int indicatorState = 0;
    private bool isIndicatorOn, isBlinking, isLightOn, isHandbrakePulled, isWiperOn, isForward, isBreaking;
    private float timer, currentZRotation;
    private Vector3 gasPedalStartPos, breakPedalStartPos, clutchPedalStartPos;


    private void Start ()
    {
        String carName = gameObject.name;

        // get pedal positions
        gasPedalStartPos = pedalGas.localPosition;
        breakPedalStartPos = pedalBreak.localPosition;
        clutchPedalStartPos = pedalClutch.localPosition;

        // set booleans
        isLightOn = false;
    }

    private void Update ()
    {
        // update functions when driving
        if (player.GetComponent<MouseLook>().isDriving)
        {
            GetInput();
            HandleSteering();
        }

        // update functions every time
        UpdateWheels();
        MonitorPerformance();
        HandleMotor();

        if (isBlinking) IndicatorLight();

        if (isWiperOn) WiperMovement();
        else WiperReset();
    }

    private void GetInput ()
    {
        // Steering Input
        horizontalInput = Input.GetAxis("Horizontal");

        // Acceleration Input
        gasInput = Input.GetAxis("Gas");
        Vector3 newPosGas = gasPedalStartPos;
        if (gasInput > 0) newPosGas.z = gasPedalStartPos.z + 0.1f * gasInput;
        pedalGas.localPosition = newPosGas;

        // Breaking Input
        breakingPower = Input.GetAxis("Break");
        Vector3 newPosBreak = breakPedalStartPos;
        newPosBreak.z = breakPedalStartPos.z + 0.1f * breakingPower;
        pedalBreak.localPosition = newPosBreak;
        wheelColliderFL.brakeTorque = breakForce * breakingPower * 0.7f;
        wheelColliderFR.brakeTorque = breakForce * breakingPower * 0.7f;

        if (!isHandbrakePulled)
        {
            wheelColliderRL.brakeTorque = breakForce * breakingPower * 0.3f;
            wheelColliderRR.brakeTorque = breakForce * breakingPower * 0.3f;
        }

        if (Input.GetAxis("Break") > 0f)
        {
            if (!breakSoundSource.isPlaying && !isBreaking)
            {
                breakSoundSource.PlayOneShot(breakClip);
            }
            isBreaking = true;
        }
        else
        {
            breakSoundSource.Stop();
            isBreaking = false;
        }

        // Clutch Input
        // clutchPower = 1 - Input.GetAxis("Clutch");
        Vector3 newPosClutch = clutchPedalStartPos;
        newPosClutch.z = clutchPedalStartPos.z + 0.1f * Input.GetAxis("Clutch");
        pedalClutch.localPosition = newPosClutch;

        // start engine
        if (Input.GetButtonDown("Start"))
        {
            IgniteEngine();
        }

        if (Input.GetButtonDown("ExitCar")) 
        {
            GetOutOfCar();
        }

        // Check for gear shift
        if (Input.GetButtonDown("NextGear"))
        {
            ShiftToNextGear();
        }
        else if (Input.GetButtonDown("PrevGear"))
        {
            ShiftToPreviousGear();
        }

        if (shifterText) shifterText.text = gearNames[currentGear];
        if (shifter) shifter.transform.localRotation = Quaternion.Euler(clutchAngles [currentGear]);

        if (Input.GetButtonDown("IndicatorLeft"))
        {
            UseIndicatorLeft();

        }
        else if (Input.GetButtonDown("IndicatorRight"))
        {
            UseIndicatorRight();
        }

        // toggle headlights
        if (Input.GetButtonDown("Light"))
        {
            TurnLightsOn();
        }

        // pull handbreak
        if (Input.GetButtonDown("Handbreak"))
        {
            PullHandbrake();
        }

        // pull wiper lever
        if (Input.GetButtonDown("Wiper"))
        {
            UseWiper();
        }
    }

    public void PullHandbrake ()
    {
        if (isHandbrakePulled)
        {
            isHandbrakePulled = false;
            handBrake.localRotation = Quaternion.Euler(0f, 0f, 0f);
            wheelColliderRL.brakeTorque = 0f;
            wheelColliderRR.brakeTorque = 0f;
            handBrakeIndicator.GetComponent<MeshRenderer>().material.DisableKeyword("_EMISSION");

            interiorSoundSource.PlayOneShot(handbrakeOffClip);
        }
        else
        {
            isHandbrakePulled = true;
            handBrake.localRotation = Quaternion.Euler(-20f, 0f, 0f);
            wheelColliderRL.brakeTorque = breakForce;
            wheelColliderRR.brakeTorque = breakForce;
            handBrakeIndicator.GetComponent<MeshRenderer>().material.EnableKeyword("_EMISSION");
            handBrakeIndicator.GetComponent<Renderer>().material.SetColor("_EmissionColor", Color.yellow);

            interiorSoundSource.PlayOneShot(handbrakeOnClip);
        }
    }

    public void ShiftToNextGear() 
    {
        if (currentGear < gearRatios.Length - 1)
        {
            currentGear++;
            interiorSoundSource.PlayOneShot(shifterUpClip);
        }
    }

    public void ShiftToPreviousGear() 
    {
        if (currentGear > 0)
        {
            currentGear--;
            interiorSoundSource.PlayOneShot(shifterDownClip);
        }
    }

    public void UseIndicatorLeft()
    {
        PlayIndicatorLoop();      

        if (indicatorState == 1)
        {
            indicatorLever.localRotation = Quaternion.Euler(0f, 0f, 0f);
            isBlinking = false;
            indicatorState = 0;
            indicatorFL.enabled = false;
            indicatorFR.enabled = false;
            indicatorRL.enabled = false;
            indicatorRR.enabled = false;
            indicatorIndicator.GetComponent<MeshRenderer>().material.DisableKeyword("_EMISSION");
            indicatorSoundSource.Stop();
        }
        else
        {
            indicatorLever.localRotation = Quaternion.Euler(0f, -15f, 0f);
            isBlinking = true;
            indicatorState = 1;
        }

        interiorSoundSource.PlayOneShot(leverPullClip);
    }

    public void UseIndicatorRight()
    {
        PlayIndicatorLoop();

        if (indicatorState == 2)
        {
            indicatorLever.localRotation = Quaternion.Euler(0f, 0f, 0f);
            isBlinking = false;
            indicatorState = 0;
            indicatorFL.enabled = false;
            indicatorFR.enabled = false;
            indicatorRL.enabled = false;
            indicatorRR.enabled = false;
            indicatorIndicator.GetComponent<MeshRenderer>().material.DisableKeyword("_EMISSION");
            indicatorSoundSource.Stop();
        }
        else
        {
            indicatorLever.localRotation = Quaternion.Euler(0f, 15f, 0f);
            isBlinking = true;
            indicatorState = 2;
        }

        interiorSoundSource.PlayOneShot(leverPullClip);
    }

    private void PlayIndicatorLoop ()
    {
        indicatorSoundSource.clip = indicatorLoopClip;
        indicatorSoundSource.loop = true;
        indicatorSoundSource.Play();
    }

    public void UseWiper()
    {
        wiperSoundSource.clip = wiperLoopClip;
        wiperSoundSource.loop = true;

        if (isWiperOn) 
        {
            isWiperOn = false;
            wiperLever.localRotation = Quaternion.Euler(0f, 0f, 0f);
            wiperSoundSource.Stop();
        }
        else 
        {
            isWiperOn = true;
            wiperLever.localRotation = Quaternion.Euler(0f, -15f, 0f);
            wiperSoundSource.Play();
        }

        interiorSoundSource.PlayOneShot(leverPullClip);
    }

    void WiperMovement()
    {
        if (isForward)
        {
            currentZRotation += wiperSpeed * Time.deltaTime;

            if (currentZRotation >= 100f)
            {
                currentZRotation = 100f;
                isForward = false;
            } 
        }
        else 
        {
            currentZRotation -= wiperSpeed * Time.deltaTime;

            if (currentZRotation <= 0f)
            {
                currentZRotation = 0f;
                isForward = true;
            } 
        }
        
        wiper.localEulerAngles = new Vector3(wiper.localEulerAngles.x, wiper.localEulerAngles.y, currentZRotation);
    }

    void WiperReset()
    {
        if (currentZRotation > 0f)
        {
            currentZRotation -= wiperSpeed * Time.deltaTime;

            if (currentZRotation <= 0f)
            {
                currentZRotation = 0f;
                isForward = true;
            } 
        } 

        wiper.localEulerAngles = new Vector3(wiper.localEulerAngles.x, wiper.localEulerAngles.y, currentZRotation);
    }

    public void TurnLightsOn()
    {
        if (isLightOn)
        {
            isLightOn = false;
            lightIndicator.GetComponent<MeshRenderer>().material.DisableKeyword("_EMISSION");

            foreach (GameObject light in lightGroup)
            {
                light.GetComponent<Light>().enabled = false;
            }

            headlights.GetComponent<MeshRenderer>().material.DisableKeyword("_EMISSION");
        }
        else
        {
            isLightOn = true;
            lightIndicator.GetComponent<MeshRenderer>().material.EnableKeyword("_EMISSION");
            lightIndicator.GetComponent<Renderer>().material.SetColor("_EmissionColor", Color.blue);

            foreach (GameObject light in lightGroup)
            {
                light.GetComponent<Light>().enabled = true;
            }

            headlights.GetComponent<MeshRenderer>().material.EnableKeyword("_EMISSION");
        }

        interiorSoundSource.PlayOneShot(lightSwitchClip);
    }

    public void TurnInteriorLightsOn ()
    {
        if (isInteriorLightOn)
        {
            isInteriorLightOn = false;
            interiorLight.GetComponent<Light>().enabled = false;
        }
        else
        {
            isInteriorLightOn = true;
            interiorLight.GetComponent<Light>().enabled = true;
        }

        interiorSoundSource.PlayOneShot(lightSwitchClip);
    }

    public void IgniteEngine ()
    {
        if (engineActive)
        {
            motorSoundSource.Stop();
            engineActive = false;
            motorSoundSource.loop = false;
            motorSoundSource.PlayOneShot(motorStopClip);
            interiorSoundSource.PlayOneShot(insertKeyClip);
        }
        else
        {
            engineActive = true;

            StartCoroutine(PlayMotorLoop());
        }
    }

    private IEnumerator PlayMotorLoop()
    {
        motorSoundSource.PlayOneShot(motorStartClip);
        
        yield return new WaitForSeconds(motorStartClip.length);
        
        motorSoundSource.clip = motorIdleClip;
        motorSoundSource.loop = true;
        motorSoundSource.Play();
    }

    private void GetOutOfCar () {
        if (player.GetComponent<MouseLook>().isDriving && leftDoor.GetComponent<UseObject>().isActive)
        {
            player.rotation = Quaternion.Euler(0f, 0f, 0f);
            player.position = exitTarget.position;
            player.GetComponent<MouseLook>().isDriving = false;
            player.parent = null;
            //player.GetComponent<CapsuleCollider>().isTrigger = false;
            player.GetComponent<CharacterController>().enabled = true;
            driveInteractionBox.GetComponent<BoxCollider>().enabled = true;
        }
    }

    public void GetInsideCar () {
        if (!player.GetComponent<MouseLook>().isDriving)
        {
            player.position = camTarget.position;
            player.rotation = camTarget.transform.rotation;
            player.GetComponent<MouseLook>().isDriving = true;
            player.parent = gameObject.transform;
            //player.GetComponent<CapsuleCollider>().isTrigger = true;
            player.GetComponent<CharacterController>().enabled = false;
            Camera.main.GetComponent<Light>().enabled = false;
            driveInteractionBox.GetComponent<BoxCollider>().enabled = false;
        }
    }

    private void IndicatorLight ()
    {       
        timer += Time.deltaTime;

        if (timer >= 0.35f)
        {
            if (isIndicatorOn)
            {
                isIndicatorOn = false;
                indicatorFL.enabled = false;
                indicatorFR.enabled = false;
                indicatorRL.enabled = false;
                indicatorRR.enabled = false;
                indicatorIndicator.GetComponent<MeshRenderer>().material.DisableKeyword("_EMISSION");
            }
            else
            {
                isIndicatorOn = true;
                if (indicatorState == 1)
                {
                    indicatorFL.enabled = true;
                    indicatorRL.enabled = true;
                    indicatorIndicator.GetComponent<MeshRenderer>().material.EnableKeyword("_EMISSION");
                    indicatorIndicator.GetComponent<Renderer>().material.SetColor("_EmissionColor", Color.green);
                }
                else if (indicatorState == 2)
                {
                    indicatorFR.enabled = true;
                    indicatorRR.enabled = true;
                    indicatorIndicator.GetComponent<MeshRenderer>().material.EnableKeyword("_EMISSION");
                }
            }
            timer = 0f;
        }
    }

    private void HandleMotor ()
    {
        // Calculate motor torque using engine RPM
        motorTorque = CalculateMotorTorque();

        // Apply motor torque to front wheel colliders
        wheelColliderFL.motorTorque = motorTorque * gasInput;
        wheelColliderFR.motorTorque = motorTorque * gasInput;
            
        // Apply braking force
        ApplyBreaking();

        // handle gas consumption
        if (engineActive)
        {
            if (gasTank.GetComponent<FuelTank>().currentLiquid > 0) gasTank.GetComponent<FuelTank>().currentLiquid -= gasConsumption * motorRPM;
            else gasTank.GetComponent<FuelTank>().currentLiquid = 0;

            // Wiggle using sine wave
            float offsetX = Mathf.Sin(Time.time * wiggleSpeed) * wiggleAmount;
            float offsetY = Mathf.Cos(Time.time * wiggleSpeed) * wiggleAmount; // Slightly offset for irregularity
            
            steeringWheel.localPosition = Vector3.zero + new Vector3(offsetX, offsetY, 0);
            motor.localPosition = Vector3.zero + new Vector3(offsetX, offsetY, 0);

            // Calculate and set the pitch
            float normalizedValue = Mathf.InverseLerp(minRPM, maxRPM, motorRPM);
            float targetPitch = Mathf.Lerp(0.75f, 1.5f, normalizedValue);
            float targetVolume = Mathf.Lerp(0.5f, 1f, normalizedValue);

            motorSoundSource.pitch = Mathf.Lerp(motorSoundSource.pitch, targetPitch, Time.deltaTime * 5f);
            motorSoundSource.volume = Mathf.Lerp(motorSoundSource.volume, targetVolume, Time.deltaTime * 5f);  
        }
    }

    // Function to calculate motor torque based on RPM and throttle input
    private float CalculateMotorTorque ()
    {
        if (gasTank.GetComponent<FuelTank>().currentLiquid <= 0f) engineActive = false;

        // motor idle state
        if (engineActive)
        {
            if (currentGear == 1)
            {
                motorRPM = Mathf.Lerp(motorRPM, Mathf.Max(minRPM, maxRPM * gasInput) + UnityEngine.Random.Range(-50, 50), Time.deltaTime);
            }
            // motor in gear
            else
            {
                wheelRPM = Mathf.Abs((wheelColliderFL.rpm + wheelColliderFR.rpm) / 2f) * gearRatios [currentGear] * differentialRatio;
                motorRPM = Mathf.Lerp(motorRPM, Mathf.Max(minRPM - 100, wheelRPM), Time.deltaTime * motorInertia);
                currentTorque = (torqueCurve.Evaluate(motorRPM / maxRPM) * motorForce / motorRPM) * gearRatios [currentGear] * differentialRatio * 5252f;
            }
        }
        // motor disengaged
        else
        {
            motorRPM = Mathf.Lerp(motorRPM, 0f, Time.deltaTime);
            currentTorque = 0f;
        }

        // Adjust torque based on throttle input
        return currentTorque;
    }

    private void MonitorPerformance ()
    {
        // monitor vehicle speed
        speed = GetComponent<Rigidbody>().linearVelocity.magnitude * 3.6f;

        // update instruments in car
        rpmNeedle.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Lerp(0f, -115, motorRPM / maxRPM));
        speedNeedle.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Lerp(0f, -266, ((speed-20) / 100)));
        fuelNeedle.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Lerp(0, -78, gasTank.GetComponent<FuelTank>().currentLiquid / gasTank.GetComponent<FuelTank>().maxLiquid));

        // moinitor performance
        monitorText.GetComponent<Text>().text = Mathf.Floor(speed) + " kmh\n" + Mathf.Floor(motorRPM) + " rpm\n" + Mathf.Floor(currentTorque) + " nm\n" + gearNames[currentGear] + "\n" + Mathf.Floor(gasTank.GetComponent<FuelTank>().currentLiquid) + "/" + Mathf.Floor(gasTank.GetComponent<FuelTank>().maxLiquid) + " l";
    }

    private void ApplyBreaking ()
    {
        wheelColliderFR.brakeTorque = breakingPower * breakForce;
        wheelColliderFL.brakeTorque = breakingPower * breakForce;
    }

    private void HandleSteering ()
    {
        // Calculate the steering angle based on the horizontal input
        currentSteerAngle = maxSteerAngle * horizontalInput;

        // Set the steer angle for the wheel colliders
        wheelColliderFL.steerAngle = currentSteerAngle;
        wheelColliderFR.steerAngle = currentSteerAngle;

        // Calculate the rotation angle for the child object (steering wheel)
        float childRotationAngle = currentSteerAngle;

        // Apply the rotation to the child object (steering wheel)
        steeringWheel.localRotation = Quaternion.Euler(steeringWheel.localRotation.x, currentSteerAngle * 8f, 0f);
    }

    private void UpdateWheels ()
    {
        UpdateSingleWheel(wheelColliderFL, wheelTransformFL);
        UpdateSingleWheel(wheelColliderFR, wheelTransformFR);
        UpdateSingleWheel(wheelColliderRR, wheelTransformRR);
        UpdateSingleWheel(wheelColliderRL, wheelTransformRL);
    }

    private void UpdateSingleWheel (WheelCollider wheelCollider, Transform wheelTransform)
    {
        Vector3 pos;
        Quaternion rot;
        wheelCollider.GetWorldPose(out pos, out rot);
        wheelTransform.rotation = rot;
        wheelTransform.position = pos;
    }
}