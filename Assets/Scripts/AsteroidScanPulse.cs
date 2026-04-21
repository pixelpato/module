using Unity.VisualScripting;
using UnityEngine;

public class AsteroidScanPulse : MonoBehaviour
{
    public float pulseDuration = 1.0f;

    private MaterialPropertyBlock mpb;
    private Renderer rend;

    private float pulseTimer = -1f;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        mpb = new MaterialPropertyBlock();
    }

    void Update()
    {
        if (pulseTimer < 0f) return;

        pulseTimer += Time.deltaTime;

        float t = pulseTimer / pulseDuration;

        if (t >= 1f)
        {
            t = 1f;
            pulseTimer = -1f; // stop
        }

        // Smooth pulse: 0 → 1 → 0
        float alpha = Mathf.Sin(t * Mathf.PI);

        ApplyFresnel(alpha);
    }

    void ApplyFresnel(float value)
    {
        rend.GetPropertyBlock(mpb);
        mpb.SetFloat("_FresnelAlpha", value);
        rend.SetPropertyBlock(mpb);
    }

    public void TriggerScanPulse()
    {
        pulseTimer = 0f;
    }

    void OnParticleCollision(GameObject other)
    {
        if (other.CompareTag("ScanWave"))
        {
            TriggerScanPulse();
        }
    }
}