using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Dialog sebagai urutan coroutine (P6): Run / Say / Narrate / Choose.
/// Run mengunci pemain (InputGate dari P4) selama urutan berjalan; monster liar ikut diam.
/// Teks diketik 55 huruf/detik dengan blip tiap 3 huruf. Klik di mana saja atau E/Spasi/Enter = lanjut.
/// </summary>
public class DialogueRunner : MonoBehaviour
{
    [SerializeField] private DialogueView view;
    [SerializeField] private float charsPerSecond = 55f;
    [SerializeField] private int blipEvery = 3;

    private InputGate gate;
    private AudioService audioService;
    private Coroutine running;
    private bool clicked;
    private int picked = -1;

    public bool IsRunning { get; private set; }
    public bool ChoicesOpen => view.ChoicesOpen;
    public string CurrentText => view.BodyText;

    public void Construct(InputGate inputGate, AudioService audio)
    {
        gate = inputGate;
        audioService = audio;
    }

    void Awake()
    {
        view.Clicked += () => clicked = true;
        view.ChoicePicked += i => picked = i;
        view.Hide();
    }

    public void Run(IEnumerator sequence)
    {
        if (!IsRunning && sequence != null) running = StartCoroutine(RunRoutine(sequence));
    }

    /// <summary>Hentikan paksa (pindah scene).</summary>
    public void Cancel()
    {
        if (running != null) StopCoroutine(running);
        running = null;
        view.Hide();
        if (IsRunning) gate?.Unlock();
        IsRunning = false;
    }

    private IEnumerator RunRoutine(IEnumerator sequence)
    {
        IsRunning = true;
        gate?.Lock();                 // pemain berhenti
        yield return null;            // lewati tombol pembuka
        clicked = false;
        yield return sequence;        // urutan milik NPC / cutscene
        view.Hide();
        yield return null;            // cegah dialog terbuka lagi
        gate?.Unlock();
        IsRunning = false;
        running = null;
    }

    public IEnumerator Say(string speaker, string text)
    {
        view.Show(false);
        yield return TypeLine(speaker, text, true);
    }

    public IEnumerator Narrate(string text)   // kotak biru
    {
        view.Show(true);
        yield return TypeLine("", text, true);
    }

    public IEnumerator Choose(string speaker, string prompt, string[] options, Action<int> result)
    {
        view.Show(false);
        yield return TypeLine(speaker, prompt, false);   // tanpa tanda lanjut
        picked = -1;
        view.ShowChoices(options);                       // TextMenu
        yield return new WaitUntil(() => picked >= 0);
        view.HideChoices();
        result?.Invoke(picked);
    }

    public IEnumerator PlayData(DialogueData data, string speaker)
    {
        if (data == null || data.Lines == null) yield break;
        foreach (string line in data.Lines) yield return Say(speaker, line);
    }

    /// <summary>Sama dengan klik layar (dipakai ClickCatcher dan DemoAutoPilot).</summary>
    public void Advance() => clicked = true;

    public void Pick(int index) => view.PickChoice(index);

    private IEnumerator TypeLine(string speaker, string text, bool waitForAdvance)
    {
        text = text ?? "";
        view.SetSpeaker(speaker);
        view.SetContinue(false);
        view.SetBody("");
        clicked = false;
        int startFrame = Time.frameCount;
        float shown = 0f;
        int lastBlip = 0;

        while (shown < text.Length)
        {
            if (Time.frameCount > startFrame && AdvancePressed()) break;   // tekan = tampilkan semua
            shown += charsPerSecond * Time.unscaledDeltaTime;
            int n = Mathf.Min(text.Length, Mathf.FloorToInt(shown));
            view.SetBody(text.Substring(0, n));
            if (n / blipEvery > lastBlip)
            {
                lastBlip = n / blipEvery;
                audioService?.PlayBlip();
            }
            yield return null;
        }
        view.SetBody(text);

        if (!waitForAdvance) yield break;
        view.SetContinue(true);
        yield return null;
        clicked = false;
        while (!AdvancePressed()) yield return null;
        view.SetContinue(false);
    }

    private bool AdvancePressed()
    {
        bool pressed = clicked || MenuInput.Confirm;   // klik / E / Spasi / Enter
        clicked = false;
        return pressed;
    }
}
