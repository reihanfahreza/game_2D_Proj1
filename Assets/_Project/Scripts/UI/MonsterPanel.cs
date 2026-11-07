using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel monster + sprite monster di dunia battle (P5). Animasi tanpa Animator:
/// idle 2 frame, HP mendekat 15% per frame, menerjang 14 px, Hurt (frame 3 putih berkedip 0,4 s),
/// Faint (turun 20 px + memudar 0,5 s), Evolve (sprite lama &lt;-&gt; baru, makin cepat).
/// </summary>
public class MonsterPanel : MonoBehaviour
{
    [SerializeField] private Text nameText;
    [SerializeField] private Text levelText;
    [SerializeField] private Text elementText;
    [SerializeField] private Text hpText;
    [SerializeField] private Image hpFill;
    [SerializeField] private Image expFill;
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private bool faceRight;           // partner menghadap lawan (flipX)
    [SerializeField] private GameObject starBadge;     // tugas P5: bintang saat jurus sangat efektif

    private MonsterInstance shown;
    private MonsterData shownData;
    private float hpShow;
    private Vector3 home;
    private float lungeStart = -10f;
    private bool hurting;
    private bool evolving;
    private bool fainted;
    private Coroutine effect;
    private float starUntil;

    public SpriteRenderer Body => body;

    void Awake()
    {
        if (body != null) home = body.transform.localPosition;
        if (starBadge != null) starBadge.SetActive(false);
    }

    /// <summary>displayAs: tampilkan spesies lama sampai animasi evolusi selesai.</summary>
    public void Show(MonsterInstance m, bool snapHp = true, MonsterData displayAs = null, int displayLevel = -1)
    {
        shown = m;
        shownData = displayAs != null ? displayAs : m.Data;
        nameText.text = shownData.DisplayName;
        levelText.text = $"Lv{(displayLevel > 0 ? displayLevel : m.Level)}";
        if (elementText != null) elementText.text = shownData.Element.ToString();
        if (snapHp) hpShow = m.CurrentHP;
        ApplyScale(shownData);
        if (body != null && !hurting && !evolving && !fainted) body.sprite = shownData.Frames[0];
    }

    private void ApplyScale(MonsterData data)
    {
        if (body == null) return;
        float w = data.Frames[0].rect.width;
        float s = w <= 16f ? 3f : w <= 24f ? 2.4f : 2f;   // skala 3 / 2,4 / 2
        body.transform.localScale = new Vector3(s, s, 1f);
        body.flipX = faceRight;
    }

    void Update()
    {
        if (shown == null) return;

        if (body != null && !hurting && !evolving && !fainted)
            body.sprite = shownData.Frames[Mathf.FloorToInt(Time.time / 0.35f) % 2];   // idle 2 frame

        float k = 1f - Mathf.Pow(0.85f, Time.deltaTime * 60f);                        // HP mendekat 15% per frame
        hpShow = Mathf.Lerp(hpShow, shown.CurrentHP, k);
        if (Mathf.Abs(hpShow - shown.CurrentHP) < 0.05f) hpShow = shown.CurrentHP;
        float fraction = shown.CurrentHP <= 0 ? 0f : Mathf.Clamp01(hpShow / shown.MaxHP);
        hpFill.fillAmount = fraction;
        hpFill.color = HpColors.For(fraction);
        if (hpText != null) hpText.text = $"{Mathf.RoundToInt(hpShow)}/{shown.MaxHP}";
        if (expFill != null) expFill.fillAmount = Mathf.Clamp01((float)shown.Exp / shown.ExpToNext);

        if (body != null && !fainted)
        {
            float p = (Time.time - lungeStart) / 0.26f;                                  // menerjang 14 px
            float dx = p < 1f ? Mathf.Sin(p * Mathf.PI) * 0.875f * (faceRight ? 1f : -1f) : 0f;
            body.transform.localPosition = home + new Vector3(dx, 0f, 0f);
        }

        if (starBadge != null) starBadge.SetActive(Time.time < starUntil && Mathf.FloorToInt(Time.time * 6f) % 2 == 0);
    }

    public void PlayLunge() => lungeStart = Time.time;

    public void FlashStar() => starUntil = Time.time + 1.2f;

    public void PlayHurt()
    {
        if (fainted || body == null) return;
        if (effect != null) StopCoroutine(effect);
        effect = StartCoroutine(Hurt());
    }

    private IEnumerator Hurt()
    {
        hurting = true;
        for (float t = 0f; t < 0.4f; t += Time.deltaTime)
        {
            bool white = Mathf.FloorToInt(t / 0.07f) % 2 == 0;
            body.sprite = white ? shownData.Frames[3] : shownData.Frames[0];
            yield return null;
        }
        hurting = false;
    }

    public void PlayFaint()
    {
        if (body == null) return;
        if (effect != null) StopCoroutine(effect);
        effect = StartCoroutine(Faint());
    }

    private IEnumerator Faint()
    {
        fainted = true;
        hurting = false;
        body.sprite = shownData.Frames[3];
        for (float t = 0f; t < 0.5f; t += Time.deltaTime)
        {
            float k = t / 0.5f;
            body.transform.localPosition = home + new Vector3(0f, -1.25f * k, 0f);  // 20 px = 1,25 unit
            body.color = new Color(1f, 1f, 1f, 1f - k);
            yield return null;
        }
        body.color = new Color(1f, 1f, 1f, 0f);
    }

    public void PlayEvolve(MonsterData from, MonsterData to, float seconds)
    {
        if (body == null) return;
        if (effect != null) StopCoroutine(effect);
        effect = StartCoroutine(Evolve(from, to, seconds));
    }

    private IEnumerator Evolve(MonsterData from, MonsterData to, float seconds)
    {
        evolving = true;
        hurting = false;
        float interval = 0.4f;
        float next = 0f;
        bool showNew = false;
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            if (t >= next)
            {
                showNew = !showNew;
                MonsterData d = showNew ? to : from;
                ApplyScale(d);
                body.sprite = Random.value < 0.2f ? d.Frames[3] : d.Frames[0];   // sesekali putih
                interval = Mathf.Max(0.05f, interval * 0.78f);                    // makin cepat
                next = t + interval;
            }
            yield return null;
        }
        ApplyScale(to);
        body.sprite = to.Frames[0];
        evolving = false;
    }
}
