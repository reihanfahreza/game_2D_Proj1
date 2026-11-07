using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tampilan battle (P5): kotak pesan (diketik 55 huruf/detik), menu 2 x 2, efek layar.
/// PlayIntro (dua tirai terbuka 0,48 s), PlayFlash (putih memudar 0,5 s), PlayEvolveRings (3 cincin, 125 px/s).
/// Tugas P5: angka damage melayang di atas sprite saat event Hit.
/// </summary>
public class BattleView : MonoBehaviour
{
    [SerializeField] private Text messageText;
    [SerializeField] private Text hintText;
    [SerializeField] private TextMenu menu;
    [SerializeField] private MonsterPanel enemyPanel;
    [SerializeField] private MonsterPanel playerPanel;
    [SerializeField] private Image[] rings;
    [SerializeField] private Image flash;
    [SerializeField] private RectTransform wipeTop;
    [SerializeField] private RectTransform wipeBottom;
    [SerializeField] private RectTransform damageLayer;
    [SerializeField] private Text damageTemplate;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private float charsPerSecond = 55f;

    private Coroutine typing;
    private int lastPick;

    public MonsterPanel EnemyPanel => enemyPanel;
    public MonsterPanel PlayerPanel => playerPanel;
    public TextMenu Menu => menu;

    public event Action<int> ActionClicked;

    void Awake()
    {
        menu.Picked += i =>
        {
            lastPick = i;
            ActionClicked?.Invoke(i);
        };
        menu.Close();
        flash.color = new Color(1f, 1f, 1f, 0f);
        foreach (Image r in rings) r.gameObject.SetActive(false);
        hintText.enabled = false;
        messageText.text = "";
        if (damageTemplate != null) damageTemplate.gameObject.SetActive(false);
    }

    public void ShowMessage(string text)
    {
        hintText.enabled = false;
        menu.Close();
        if (typing != null) StopCoroutine(typing);
        typing = StartCoroutine(Type(text));
    }

    private IEnumerator Type(string text)
    {
        for (float shown = 0f; shown < text.Length; shown += charsPerSecond * Time.unscaledDeltaTime)
        {
            messageText.text = text.Substring(0, Mathf.FloorToInt(shown));
            yield return null;
        }
        messageText.text = text;
        typing = null;
    }

    public void ShowMenu(IReadOnlyList<string> labels, IReadOnlyList<bool> usable, string prompt, string hint)
    {
        if (typing != null) StopCoroutine(typing);
        typing = null;
        messageText.text = prompt;            // "Apa yang akan Pyrit lakukan?"
        hintText.text = hint;                 // "Jurus elemen: Semburan Api"
        hintText.enabled = true;
        int start = lastPick < usable.Count && usable[lastPick] ? lastPick : 0;   // ingat pilihan terakhir
        menu.Open(new List<string>(labels).ToArray(), new List<bool>(usable).ToArray(), start);
    }

    public void HideMenu() => menu.Close();

    public void PlayIntro() => StartCoroutine(Intro());

    private IEnumerator Intro()
    {
        float half = 90f * UiTheme.Px;
        for (float t = 0f; t < 0.48f; t += Time.deltaTime)
        {
            float k = 1f - Mathf.Pow(1f - t / 0.48f, 2f);
            wipeTop.anchoredPosition = new Vector2(0f, k * half);
            wipeBottom.anchoredPosition = new Vector2(0f, -90f * UiTheme.Px - k * half);
            yield return null;
        }
        wipeTop.gameObject.SetActive(false);
        wipeBottom.gameObject.SetActive(false);
    }

    public void PlayFlash() => StartCoroutine(Flash());

    private IEnumerator Flash()
    {
        for (float t = 0f; t < 0.5f; t += Time.deltaTime)
        {
            flash.color = new Color(1f, 1f, 1f, 1f - t / 0.5f);
            yield return null;
        }
        flash.color = new Color(1f, 1f, 1f, 0f);
    }

    public void PlayEvolveRings(float seconds) => StartCoroutine(EvolveRings(seconds));

    private IEnumerator EvolveRings(float seconds)
    {
        Vector2 center = ScreenPointOf(playerPanel.Body.bounds.center);
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            for (int i = 0; i < rings.Length; i++)
            {
                float local = t - i * 0.35f;
                bool on = local > 0f;
                rings[i].gameObject.SetActive(on);
                if (!on) continue;
                float size = (8f + (local * 125f) % 120f) * 2f * UiTheme.Px;   // 125 px/s
                RectTransform rt = rings[i].rectTransform;
                rt.anchoredPosition = center;
                rt.sizeDelta = new Vector2(size, size);
                rings[i].color = new Color(1f, 0.78f, 0.1f, 1f - ((local * 125f) % 120f) / 120f);
            }
            yield return null;
        }
        foreach (Image r in rings) r.gameObject.SetActive(false);
    }

    public void ShowDamage(MonsterPanel panel, int damage, bool critical)
    {
        if (damageTemplate == null || panel.Body == null) return;
        Bounds b = panel.Body.bounds;
        Vector2 pos = ScreenPointOf(new Vector3(b.center.x, b.max.y, 0f));
        Text t = Instantiate(damageTemplate, damageLayer);
        t.gameObject.SetActive(true);
        t.text = critical ? $"{damage}!" : damage.ToString();
        t.color = critical ? UiTheme.Gold : Color.white;
        StartCoroutine(FloatUp(t, pos));
    }

    private IEnumerator FloatUp(Text t, Vector2 start)
    {
        RectTransform rt = t.rectTransform;
        for (float time = 0f; time < 0.8f; time += Time.unscaledDeltaTime)
        {
            float k = time / 0.8f;
            rt.anchoredPosition = start + new Vector2(0f, 14f * UiTheme.Px * k);
            Color c = t.color;
            c.a = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
            t.color = c;
            yield return null;
        }
        Destroy(t.gameObject);
    }

    /// <summary>Posisi dunia -> koordinat lokal damageLayer (anchor tengah).</summary>
    private Vector2 ScreenPointOf(Vector3 world)
    {
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(worldCamera, world);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(damageLayer, screen, null, out Vector2 local);
        return local;
    }
}
