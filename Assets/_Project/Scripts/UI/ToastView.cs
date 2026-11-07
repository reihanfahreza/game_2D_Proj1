using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Pesan singkat di atas layar (P6), panel emas UI_Panel_Gold. Pesan mengantre.</summary>
public class ToastView : MonoBehaviour
{
    [SerializeField] private RectTransform panel;
    [SerializeField] private Text label;
    [SerializeField] private CanvasGroup group;
    [SerializeField] private float holdSeconds = 2.4f;

    private readonly Queue<string> queue = new Queue<string>();
    private Coroutine running;

    public string Current { get; private set; }

    void Awake() => group.alpha = 0f;

    public void Show(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        queue.Enqueue(text);
        if (running == null) running = StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        while (queue.Count > 0)
        {
            Current = queue.Dequeue();
            label.text = Current;
            float width = Mathf.Clamp(label.preferredWidth / UiTheme.Px + 16f, 60f, 300f);
            UiTheme.Place(panel, (320f - width) / 2f, 42f, width, 17f);
            yield return Fade(0f, 1f, 0.2f);
            yield return new WaitForSecondsRealtime(holdSeconds);
            yield return Fade(1f, 0f, 0.3f);
        }
        Current = null;
        running = null;
    }

    private IEnumerator Fade(float from, float to, float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            group.alpha = Mathf.Lerp(from, to, t / seconds);
            yield return null;
        }
        group.alpha = to;
    }
}
