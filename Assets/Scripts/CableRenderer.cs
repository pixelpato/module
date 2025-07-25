using UnityEngine;


public class CableRenderer : MonoBehaviour
{
    public bool isStaticCable;
    public Transform [] cableStart;
    public Transform [] cableEnd;

    [SerializeField] private int segments = 6;
    [SerializeField] private float sagAmount = 0.5f;
    [SerializeField] private float cableWidth = 0.1f;

    private Vector3 [] positions;
    private LineRenderer lineRenderer;


    void Start ()
    {
        GetCablePoints ();
    }

    public void GetCablePoints ()
    {
        positions = new Vector3[segments + 1];

        if (cableStart[0] != null && cableEnd[0] != null)
        {
            for (int i = 0; i < cableStart.Length; i++)
            {
                DrawCable(cableStart[i], cableEnd[i]);
            }
        }
    }

    private void Update ()
    {
        if (!isStaticCable)
        {
            if (cableStart [0] != null && cableEnd [0] != null)
            {
                for (int i = 0; i < cableStart.Length; i++)
                {
                    UpdatePositions(cableStart [i], cableEnd [i]);
                }
            }
        }
    }

    private void DrawCable (Transform pointA, Transform pointB)
    {
        // Create a new GameObject and add a Line Renderer component to it
        GameObject cable = new GameObject("Cable");
        lineRenderer = cable.AddComponent<LineRenderer>();
        lineRenderer.transform.parent = this.transform;

        // Configure the Line Renderer (adjust these values as needed)
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.black;
        lineRenderer.endColor = Color.black;
        lineRenderer.startWidth = cableWidth;
        lineRenderer.endWidth = cableWidth;

        // Set the number of points in the line renderer
        lineRenderer.positionCount = segments + 1;

        // Calculate the midpoint to create the sag effect
        Vector3 midPoint = (pointA.position + pointB.position) / 2;
        midPoint.y -= sagAmount; // Move the midpoint down to create sag

        for (int i = 0; i <= segments; i++)
        {
            float t = (float) i / segments;
            // Calculate the quadratic Bezier point
            positions [i] = CalculateBezierPoint(t, pointA.position, midPoint, pointB.position);
        }

        // Set the positions to the line renderer
        lineRenderer.SetPositions(positions);
    }

    void UpdatePositions (Transform pointA, Transform pointB)
    {
        // Calculate the midpoint to create the sag effect
        Vector3 midPoint = (pointA.position + pointB.position) / 2;
        midPoint.y -= sagAmount; // Move the midpoint down to create sag

        for (int i = 0; i <= segments; i++)
        {
            float t = (float) i / segments;
            // Calculate the quadratic Bezier point
            positions [i] = CalculateBezierPoint(t, pointA.position, midPoint, pointB.position);
        }

        // Set the positions to the line renderer
        lineRenderer.SetPositions(positions);
    }

    Vector3 CalculateBezierPoint (float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        // Bezier formula: B(t) = (1-t)^2 * p0 + 2(1-t)t * p1 + t^2 * p2
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        Vector3 p = uu * p0; // (1-t)^2 * p0
        p += 2 * u * t * p1; // 2(1-t)t * p1
        p += tt * p2; // t^2 * p2
        return p;
    }
}

