using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Gua gelap (P7). Masuk trigger -> Global Light turun ke 0,15. Semua cahaya tambahan
/// (cahaya pemain, kristal, BossLight) mengikuti gelapnya gua: di luar gua dark = 0, kristal mati.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LightZone : MonoBehaviour
{
    [SerializeField] private Light2D globalLight;
    [SerializeField] private Light2D playerLight;
    [SerializeField] private Light2D[] extraLights;     // versi demo
    [SerializeField] private float insideIntensity = 0.15f;
    [SerializeField] private float fadeSpeed = 2f;

    private float target = 1f;

    void OnTriggerEnter2D(Collider2D o)
    {
        if (o.CompareTag("Player")) target = insideIntensity;
    }

    void OnTriggerExit2D(Collider2D o)
    {
        if (o.CompareTag("Player")) target = 1f;
    }

    void Update()
    {
        globalLight.intensity = Mathf.MoveTowards(globalLight.intensity, target, fadeSpeed * Time.deltaTime);
        float dark = 1f - globalLight.intensity;
        if (playerLight != null) playerLight.intensity = dark;
        foreach (Light2D l in extraLights)
            if (l != null) l.intensity = dark;
    }
}
