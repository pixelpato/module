using UnityEngine;

public class Asteroid : MonoBehaviour
{
    public MineralLayer[] layers;
    public float scrollSpeed = 0.2f;

    public float[] Scan(Vector3 hitPoint, Vector3 scannerPosition)
    {
        float dist = Vector3.Distance(scannerPosition, hitPoint);
        float strength = Mathf.Pow(Mathf.Clamp01(1f - dist / 50f), 0.5f);

        float[] waveform = new float[128];

        bool hasMinerals = false;

        if (layers != null)
        {
            foreach (var layer in layers)
            {
                if (layer != null && layer.quantity > 0.01f)
                {
                    hasMinerals = true;
                    break;
                }
            }
        }

        for (int i = 0; i < waveform.Length; i++)
        {
            float t = (i / (float)waveform.Length) + Time.time * scrollSpeed;

            if (hasMinerals)
            {
                float value = 0f;

                foreach (var layer in layers)
                {
                    if (layer == null) continue;

                    // 1. AMPLITUDE from quantity
                    float amplitude = layer.quantity;

                    // 2. FREQUENCY from type
                    float frequency = GetFrequencyFromType(layer.type);

                    if (layer.type == 2) // crystal
                    {
                        value += Mathf.Sin(t * frequency) * amplitude;
                        value += Mathf.Sin(t * frequency * 0.5f) * amplitude * 0.5f;
                    }
                    else
                    {
                        value += Mathf.Sin(t * frequency) * amplitude;
                    }
                }

                // 3. NOISE from quality (inverted)
                float avgQuality = GetAverageQuality();
                float noiseAmount = (1f - avgQuality) * 0.2f;

                float noise = Mathf.PerlinNoise(Time.time * 3f, i * 0.2f);
                noise = (noise - 0.5f) * noiseAmount;

                waveform[i] = Mathf.Clamp((value + noise) * strength, -1f, 1f);
            }
            else
            {
                // very faint space noise
                float baseWave = Mathf.Sin(t * 4f) * 0.01f;

                float noise = Mathf.PerlinNoise(Time.time * 1f, i * 0.1f);
                noise = (noise - 0.5f) * 0.01f;

                waveform[i] = (baseWave + noise) * strength;
            }
        }

        return waveform;
    }

    float GetFrequencyFromType(int type)
    {
        switch (type)
        {
            case 0: return 6f;   // e.g. iron (low freq)
            case 1: return 12f;  // e.g. gold (medium)
            case 2: return 24f;  // e.g. crystal (high)
            case 3: return 40f;  // exotic
            default: return 10f;
        }
    }

    float GetAverageQuality()
    {
        if (layers == null || layers.Length == 0)
            return 0f;

        float total = 0f;
        int count = 0;

        foreach (var layer in layers)
        {
            if (layer == null) continue;

            total += layer.quality;
            count++;
        }

        return count > 0 ? total / count : 0f;
    }
}