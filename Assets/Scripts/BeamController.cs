using UnityEngine;

public class ScannerController : MonoBehaviour
{
    public float scanDistance = 50f;
    public LayerMask asteroidLayer;
    public LineRenderer beamVisual;
    public WaveformDisplay waveformDisplay;

    private Asteroid currentAsteroid;

    void Update()
    {
        Vector2 input = new Vector2(
            Input.GetAxis("RightStickHorizontal"),
            Input.GetAxis("RightStickVertical")
        );

        if (input.magnitude < 0.2f)
        {
            beamVisual.enabled = false;
            waveformDisplay.HideWaveform();
            return;
        }

        beamVisual.enabled = true;

        Vector3 direction = new Vector3(input.x, 0, input.y).normalized;

        Ray ray = new Ray(transform.position, direction);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, scanDistance, asteroidLayer))
        {
            currentAsteroid = hit.collider.GetComponent<Asteroid>();

            beamVisual.SetPosition(0, transform.position);
            beamVisual.SetPosition(1, hit.point);

            if (currentAsteroid != null)
            {
                float[] data = currentAsteroid.Scan(hit.point, transform.position);

                waveformDisplay.ShowWaveform(data);
            }
        }
        else
        {
            currentAsteroid = null;

            beamVisual.SetPosition(0, transform.position);
            beamVisual.SetPosition(1, transform.position + direction * scanDistance);

            float[] data = GenerateSpaceNoise(128);

            waveformDisplay.ShowWaveform(data);
        }
    }

    float[] GenerateSpaceNoise(int length)
    {
        float[] data = new float[length];

        for (int i = 0; i < length; i++)
        {
            float t = i / (float)length;

            float baseWave = Mathf.Sin(t * .6f) * 0.0015f;
            float noise = Mathf.PerlinNoise(Time.time * 1.5f, i * 0.15f);
            
            noise = (noise - 0.5f) * 0.002f;

            data[i] = baseWave + noise;
        }

        return data;
    }
}