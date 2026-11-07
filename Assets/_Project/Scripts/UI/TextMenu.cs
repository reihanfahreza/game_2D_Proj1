using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Menu teks yang dipakai ulang (P5): judul, battle, dialog pilihan, menu jeda.
/// Panah/WASD untuk pindah, Enter/Spasi/E untuk memilih, klik &amp; hover mouse juga bisa.
/// Pilihan redup (Disabled #5C5680) tidak bisa dipilih. Bunyi pindah/ok lewat TextMenu.Sfx (P6).
/// </summary>
public class TextMenu : MonoBehaviour
{
    /// <summary>Diisi GameBootstrap (P6, AudioService).</summary>
    public static AudioService Sfx;

    [SerializeField] private Text[] rows;
    [SerializeField] private int columns = 1;
    [SerializeField] private Color normal = new Color32(237, 233, 247, 255);    // Text #EDE9F7
    [SerializeField] private Color selected = new Color32(255, 198, 26, 255);   // Gold #FFC61A
    [SerializeField] private Color disabled = new Color32(92, 86, 128, 255);    // Disabled #5C5680

    private string[] labels = Array.Empty<string>();
    private bool[] usable = Array.Empty<bool>();
    private int count;
    private bool acceptInput;

    public int Selected { get; private set; }
    public bool IsOpen => count > 0 && gameObject.activeInHierarchy;

    public event Action<int> Picked;
    public event Action Cancelled;

    void Awake()
    {
        for (int i = 0; i < rows.Length; i++)
        {
            int index = i;   // salin i ke variabel lokal sebelum lambda

            Button button = rows[i].GetComponent<Button>();
            if (button == null) button = rows[i].gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() => Click(index));

            EventTrigger trigger = rows[i].GetComponent<EventTrigger>();
            if (trigger == null) trigger = rows[i].gameObject.AddComponent<EventTrigger>();
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => Hover(index));
            trigger.triggers.Add(enter);
        }
    }

    public void Open(string[] options, bool[] enabled = null, int start = 0)
    {
        labels = options ?? Array.Empty<string>();
        count = Mathf.Min(labels.Length, rows.Length);
        usable = new bool[count];
        for (int i = 0; i < count; i++) usable[i] = enabled == null || (i < enabled.Length && enabled[i]);
        for (int i = 0; i < rows.Length; i++) rows[i].gameObject.SetActive(i < count);

        Selected = Mathf.Clamp(start, 0, Mathf.Max(0, count - 1));
        if (count > 0 && !usable[Selected]) Selected = FirstUsable();
        gameObject.SetActive(true);
        acceptInput = false;            // abaikan tombol pembuka
        Redraw();
    }

    public void Close()
    {
        count = 0;
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (count == 0) return;
        if (!acceptInput) { acceptInput = true; return; }

        if (MenuInput.Up) Move(-columns);
        else if (MenuInput.Down) Move(columns);
        else if (columns > 1 && MenuInput.Left) Move(-1);
        else if (columns > 1 && MenuInput.Right) Move(1);

        if (MenuInput.Confirm) Pick(Selected);
        else if (MenuInput.Cancel) Cancelled?.Invoke();
    }

    private void Move(int delta)
    {
        int next = Selected + delta;
        if (columns == 1) next = (next % count + count) % count;
        else if (next < 0 || next >= count) return;
        if (next == Selected) return;
        Selected = next;
        Sfx?.PlayUiMove();
        Redraw();
    }

    /// <summary>Pilih baris (keyboard, klik, atau DemoAutoPilot).</summary>
    public void Pick(int index)
    {
        if (count == 0 || index < 0 || index >= count) return;
        Selected = index;
        Redraw();
        if (!usable[index])
        {
            Sfx?.PlayUiCancel();
            return;
        }
        Sfx?.PlayUiConfirm();
        Picked?.Invoke(index);
    }

    private void Click(int index)
    {
        if (acceptInput) Pick(index);
        // Baris yang diklik jangan tetap "selected": Enter (Submit) bisa memicu klik kedua.
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    private void Hover(int index)
    {
        if (index >= count || index == Selected) return;
        Selected = index;
        Redraw();
    }

    private int FirstUsable()
    {
        for (int i = 0; i < count; i++)
            if (usable[i]) return i;
        return 0;
    }

    private void Redraw()
    {
        for (int i = 0; i < count; i++)
        {
            bool on = i == Selected;
            rows[i].text = (on ? "▶ " : "   ") + labels[i];
            rows[i].color = !usable[i] ? disabled : on ? selected : normal;
        }
    }
}
