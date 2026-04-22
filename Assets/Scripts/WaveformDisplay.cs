using UnityEngine;
using UnityEngine.Android;

public class WaveformDisplay : MonoBehaviour
{
    public LineRenderer line;
    public float amplitude = 3f;
    public float length = 1f;

    public float fadeSpeed = 3f;
    private float currentAlpha = 0f;
    private float targetAlpha = 0f;

    float[] currentData;
    float[] targetData;
    float[] incomingData;


    void Update()
    {
        // Fade logic (unchanged)
        currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * fadeSpeed);

        Color c = line.startColor;
        c.a = currentAlpha;
        line.startColor = c;
        line.endColor = c;

        if (incomingData != null)
        {
            if (targetData == null || targetData.Length != incomingData.Length)
                targetData = new float[incomingData.Length];

            for (int i = 0; i < incomingData.Length; i++)
            {
                targetData[i] = Mathf.Lerp(targetData[i], incomingData[i], Time.deltaTime * 5f);
            }
        }

        // Smooth waveform
        if (targetData != null)
        {
            if (currentData == null)
            {
                currentData = new float[targetData.Length];
            }

            if (currentData.Length != targetData.Length)
            {
                System.Array.Resize(ref currentData, targetData.Length);
            }

            for (int i = 0; i < targetData.Length; i++)
            {
                currentData[i] = Mathf.Lerp(currentData[i], targetData[i], Time.deltaTime * 5f);
            }

            DrawWaveform(currentData);
        }
    }

    public void ShowWaveform(float[] data)
    {
        targetAlpha = 1f;
        incomingData = data;
    }

    void DrawWaveform(float[] data)
    {
        line.positionCount = data.Length;

        float width = length;
        float step = width / data.Length;

        for (int i = 0; i < data.Length; i++)
        {
            float x = (i * step) - (width / 2f);

            float y = data[i] * amplitude;

            // subtle visual noise (small!)
            float noise = Mathf.PerlinNoise(Time.time * 3f, i * 0.1f);
            noise = (noise - 0.5f) * 0.01f;

            y += noise;

            line.SetPosition(i, new Vector3(x, y, 0));
        }
    }

    public void HideWaveform()
    {
        targetAlpha = 0f;
    }

    public void UpdateWaveform(float[] data)
    {
        if (data == null || data.Length == 0) return;

        line.positionCount = data.Length;

        float width = length;
        float step = width / data.Length;

        for (int i = 0; i < data.Length; i++)
        {
            float x = (i * step) - (width / 2f);
            float y = data[i] * amplitude;

            line.SetPosition(i, new Vector3(x, y, 0));
        }
    }
}