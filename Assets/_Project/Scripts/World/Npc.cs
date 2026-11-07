using System.Collections;
using UnityEngine;

/// <summary>
/// NPC dasar (P3, diubah P6). : MonoBehaviour, IInteractable = satu induk, boleh banyak interface.
/// Interact menoleh ke pemain lalu menjalankan Talk() lewat DialogueRunner.
/// Talk() virtual ditulis ulang oleh turunan: StarterGiver, GuardNpc, MerchantNpc, SariNpc.
/// </summary>
public class Npc : MonoBehaviour, IInteractable
{
    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");

    [SerializeField] private string npcName = "Warga";
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private Sprite faceDown, faceLeft, faceRight, faceUp;
    [SerializeField] private Animator animator;          // opsional: AOC_Npc_* (tugas P3)
    [SerializeField] private DialogueData dialogue;       // versi dasar (dipakai Sari)

    public string NpcName => npcName;

    protected DialogueRunner Dialogue { get; private set; }
    protected StoryFlags Flags { get; private set; }
    protected GameServices Services { get; private set; }

    void Awake() => Face(Vector2.down);

    public void Construct(GameServices services)
    {
        Services = services;
        Dialogue = services.Dialogue;
        Flags = services.Flags;
        OnConstructed();
    }

    public void Interact(PlayerInteractor who)
    {
        Face(who.transform.position - transform.position);
        if (Dialogue == null)
        {
            Debug.Log($"{npcName}: Halo!");   // versi P3 (tanpa Bootstrap)
            return;
        }
        if (Dialogue.IsRunning) return;
        Dialogue.Run(Talk());
    }

    /// <summary>Versi dasar: DialogueData biasa.</summary>
    protected virtual IEnumerator Talk()
    {
        if (dialogue != null) yield return Dialogue.PlayData(dialogue, npcName);
        else yield return Dialogue.Say(npcName, "Halo!");
    }

    protected virtual void OnConstructed() { }

    protected virtual void OnDestroy() { }

    public void Face(Vector2 d)
    {
        if (d == Vector2.zero) return;
        Vector2 dir = Mathf.Abs(d.x) > Mathf.Abs(d.y)
            ? new Vector2(Mathf.Sign(d.x), 0f)
            : new Vector2(0f, Mathf.Sign(d.y));

        if (body != null)
            body.sprite = dir.x != 0f ? (dir.x > 0f ? faceRight : faceLeft) : (dir.y > 0f ? faceUp : faceDown);

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetFloat(MoveX, dir.x);
            animator.SetFloat(MoveY, dir.y);
        }
    }
}
