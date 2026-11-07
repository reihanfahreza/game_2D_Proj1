using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Kotak dialog gaya demo (P6) di Canvas_Dialogue (Scn_Bootstrap, order 20).
/// Narasi: sprite UI_Panel_Narr, tanpa nama, Canvas order 110 (di atas Fader 100).
/// Label nama emas: lebar min(488, huruf x 20 + 64).
/// </summary>
public class DialogueView : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private GameObject root;
    [SerializeField] private Button clickCatcher;
    [SerializeField] private Image box;
    [SerializeField] private Text body;
    [SerializeField] private RectTransform speakerTag;
    [SerializeField] private Text speaker;
    [SerializeField] private Image continueHint;
    [SerializeField] private TextMenu choices;
    [SerializeField] private Sprite navySprite;
    [SerializeField] private Sprite narrSprite;
    [SerializeField] private int normalOrder = 20;
    [SerializeField] private int narrateOrder = 110;

    private bool narrating;
    private bool showContinue;

    public event Action Clicked;
    public event Action<int> ChoicePicked;

    public bool ChoicesOpen => choices.IsOpen;
    public string BodyText => body.text;

    void Awake()
    {
        clickCatcher.onClick.AddListener(() =>
        {
            Clicked?.Invoke();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        });
        choices.Picked += i => ChoicePicked?.Invoke(i);
    }

    void Update()
    {
        if (continueHint != null)
            continueHint.enabled = showContinue && Mathf.FloorToInt(Time.unscaledTime / 0.3f) % 2 == 0;   // berkedip 0,3 s
    }

    public void Show(bool narrate)
    {
        narrating = narrate;
        root.SetActive(true);
        box.sprite = narrate ? narrSprite : navySprite;
        canvas.sortingOrder = narrate ? narrateOrder : normalOrder;
        UiTheme.Place(box.rectTransform, 5f, narrate ? 66f : 127f, 310f, 48f);
        if (narrate) speakerTag.gameObject.SetActive(false);
    }

    public void Hide()
    {
        root.SetActive(false);
        choices.Close();
        showContinue = false;
        narrating = false;
        canvas.sortingOrder = normalOrder;
    }

    public void SetSpeaker(string speakerName)
    {
        bool has = !narrating && !string.IsNullOrEmpty(speakerName);
        speakerTag.gameObject.SetActive(has);
        if (!has) return;
        speaker.text = speakerName;
        float width = Mathf.Min(488f, speakerName.Length * 20f + 64f);
        speakerTag.sizeDelta = new Vector2(width, speakerTag.sizeDelta.y);
    }

    public void SetBody(string text) => body.text = text;

    public void SetContinue(bool on) => showContinue = on;

    public void ShowChoices(string[] options) => choices.Open(options);

    public void HideChoices() => choices.Close();

    public void PickChoice(int index) => choices.Pick(index);
}
