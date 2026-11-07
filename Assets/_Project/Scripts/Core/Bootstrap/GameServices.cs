using UnityEngine;

/// <summary>
/// Semua service yang dibuat Composition Root (GameBootstrap) dan dibagikan ke scene lewat SceneInstaller.
/// P4: Input, Database, Party, Battle, Scenes. P6: Flags, Save, Dialogue, Toast, Audio.
/// </summary>
public class GameServices
{
    public InputGate Input { get; set; }
    public MonsterDatabase Database { get; set; }
    public PlayerParty Party { get; set; }
    public BattleSession Battle { get; set; }
    public SceneFlow Scenes { get; set; }
    public StoryFlags Flags { get; set; }
    public SaveService Save { get; set; }
    public DialogueRunner Dialogue { get; set; }
    public ToastView Toast { get; set; }
    public AudioService Audio { get; set; }

    /// <summary>Diisi WorldInstaller selama Scn_World terbuka (dipakai menu jeda untuk menyimpan posisi).</summary>
    public Transform Player { get; set; }
}
