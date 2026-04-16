using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Ship Parts")]
    public Rigidbody rb;
    public ParticleSystem fire2Particles;

    [Header("Ship Controls")]
    public float thrustForce = 8.0f;
    public float rotationSpeed = 100.0f;

    float thrustAxis;
    float horizontal;

    void Update()
    {
        // Cache input
        thrustAxis = Input.GetAxis("Fire1");
        horizontal = Input.GetAxis("Horizontal");

        // Fire particle once on button press
        if (Input.GetButtonDown("Fire2"))
        {
            FireSecondary();
        }
    }

    private void FixedUpdate()
    {
        // Thrust
        if (thrustAxis > 0.01f)
        {
            rb.AddForce(transform.up * thrustForce * thrustAxis);
        }

        // Rotation
        if (Mathf.Abs(horizontal) > 0.01f)
        {
            rb.AddTorque(Vector3.forward * -horizontal * rotationSpeed);
        }
    }

    void FireSecondary()
    {
        if (fire2Particles != null)
        {
            fire2Particles.Emit(1);
        }
    }
}