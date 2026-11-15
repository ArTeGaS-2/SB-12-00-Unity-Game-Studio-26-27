using UnityEngine;

public class LightFlicker : MonoBehaviour
{
    public float baseIntensity = 10f;
    public float variation = 1.6f;
    public float speed = 7f;
    public float noiseOffset;
    Light sourceLight;

    void Awake()
    {
        sourceLight = GetComponent<Light>();
        if (sourceLight != null) baseIntensity = sourceLight.intensity;
        if (noiseOffset == 0f) noiseOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        if (sourceLight == null) return;
        float noise = Mathf.PerlinNoise(noiseOffset, Time.time * speed);
        float pulse = Mathf.Lerp(1f - variation / 10f, 1f + variation / 10f, noise);
        sourceLight.intensity = baseIntensity * pulse;
    }
}
