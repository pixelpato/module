using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipController : MonoBehaviour
{
    public GameObject scanWave;

    public float thrustForce = 20f;
    public float torqueForce = 10f;
    public float dampingStrength = 5f;

    public float maxScanRadius = 200f;
    public float scanSpeed = 20f;

    private bool isScanning = false;

    private Rigidbody rb;
    public LayerMask asteroidLayer;
    private HashSet<Collider> alreadyHit = new HashSet<Collider>();


    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
    }

    void Update()
    {
        // Fire particle once on button press
        if (Input.GetButtonDown("Scanner"))
        {
            FireScanner();
        }
    }

    void FixedUpdate()
    {
        // Input
        float thrustInput = Input.GetAxis("RT");   // 0 → 1
        float turnInput = Input.GetAxis("Horizontal"); // -1 → 1
        float dampingInput = Input.GetAxis("LT"); // 0 → 1

        // --- THRUST ---
        Vector3 forward = transform.forward;
        rb.AddForce(forward * thrustInput * thrustForce, ForceMode.Acceleration);

        // --- TORQUE ---
        rb.AddTorque(Vector3.up * turnInput * torqueForce, ForceMode.Acceleration);

        // --- DAMPING ---
        if (dampingInput > 0f)
        {
            // Damping
            rb.linearVelocity *= 1f / (1f + dampingInput * dampingStrength * Time.fixedDeltaTime);
            rb.angularVelocity *= 1f / (1f + dampingInput * dampingStrength * Time.fixedDeltaTime);
        }

        // Optional: lock movement to XZ plane
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.angularVelocity = new Vector3(0f, rb.angularVelocity.y, 0f);
    }

    void FireScanner()
    {
        if (isScanning) return;

        StartCoroutine(ScanRoutine());
    }

    IEnumerator ScanRoutine()
    {
        float radius = 0f;

        isScanning = true;

        scanWave.transform.localScale = Vector3.one;
        alreadyHit.Clear();

        while (radius < maxScanRadius)
        {
            radius += scanSpeed * Time.deltaTime;

            float diameter = radius * 2f;
            scanWave.transform.localScale = Vector3.one * diameter;

            Collider[] hits = Physics.OverlapSphere(
                transform.position,
                radius,
                asteroidLayer
            );

            foreach (var hit in hits)
            {
                if (alreadyHit.Contains(hit)) continue;

                alreadyHit.Add(hit);

                var asteroid = hit.GetComponent<AsteroidScanPulse>();
                if (asteroid != null)
                {
                    asteroid.TriggerScanPulse();
                }
            }

            yield return null;
        }

        scanWave.transform.localScale = Vector3.one;

        isScanning = false;
    }
}