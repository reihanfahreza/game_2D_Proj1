using System.Collections;
using UnityEngine;

/// <summary>Layar hitam di Scn_Bootstrap (Canvas order 100). Mulai hitam, lalu SceneFlow memudarkannya.</summary>
public class ScreenFader : MonoBehaviour
{
    [SerializeField] private CanvasGroup group;

    public bool IsOpaque => group.alpha >= 0.99f;

    void Awake()
    {
        group.alpha = 1f;
        group.blocksRaycasts = true;
    }

    public IEnumerator FadeOut(float seconds) => FadeTo(1f, seconds);
    public IEnumerator FadeIn(float seconds) => FadeTo(0f, seconds);

    private IEnumerator FadeTo(float target, float seconds)
    {
        group.blocksRaycasts = true;
        float start = group.alpha;
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            group.alpha = Mathf.Lerp(start, target, t / seconds);
            yield return null;
        }
        group.alpha = target;
        group.blocksRaycasts = target > 0.5f;
    }
}
