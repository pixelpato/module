using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Rigidbody targetRb;

    public float smoothSpeed = 0.125f;
    public Vector3 offset;

    [Header("Zoom Settings")]
    public float minZoom = 5f;
    public float maxZoom = 10f;
    public float maxSpeed = 20f; // speed at which max zoom is reached
    public float zoomSmoothSpeed = 5f;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        // --- Position follow ---
        Vector3 desiredPos = target.position + offset;
        Vector3 smoothedPos = Vector3.Lerp(transform.position, desiredPos, smoothSpeed);
        transform.position = smoothedPos;

        // --- Speed-based zoom ---
        float speed = targetRb.linearVelocity.magnitude;

        // Normalize speed (0 → 1)
        float t = Mathf.Clamp01(speed / maxSpeed);

        // Interpolate zoom
        float targetZoom = Mathf.Lerp(minZoom, maxZoom, t);

        // Smooth zoom transition
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, Time.deltaTime * zoomSmoothSpeed);
    }
}