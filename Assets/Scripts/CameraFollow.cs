using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Rigidbody targetRb;

    [Header("Follow")]
    public float followSmoothTime = 0.2f;
    private Vector3 velocity;

    [Header("Zoom")]
    public float minHeight = 10f;
    public float maxHeight = 25f;
    public float maxSpeed = 20f; // speed at which zoom is maxed
    public float zoomSmoothTime = 0.3f;

    private float currentHeightVelocity;


    void LateUpdate()
    {
        if (!target) return;

        // Get target position + look ahead point
        Vector3 lookAhead = targetRb.linearVelocity * 0.5f;
        Vector3 targetPos = target.position + new Vector3(lookAhead.x, 0f, lookAhead.z);

        // Target speed influences camera height/zoom
        float speed = targetRb.linearVelocity.magnitude;

        float t = Mathf.Clamp01(speed / maxSpeed);
        t = t * t;
        float desiredHeight = Mathf.Lerp(minHeight, maxHeight, t);

        // Smooth height
        float smoothHeight = Mathf.SmoothDamp(
            transform.position.y,
            desiredHeight,
            ref currentHeightVelocity,
            zoomSmoothTime
        );

        // Final cam position
        Vector3 desiredPosition = new Vector3(
            targetPos.x,
            smoothHeight,
            targetPos.z
        );

        // Smooth follow
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref velocity,
            followSmoothTime
        );
    }
}