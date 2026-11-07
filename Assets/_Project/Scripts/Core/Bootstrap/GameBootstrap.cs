using UnityEngine;

/// <summary>
/// Composition Root (P2-P6): satu-satunya tempat service dibuat dengan new.
/// Scn_Bootstrap selalu hidup; scene lain dimuat di atasnya oleh SceneFlow.
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private string firstScene = SceneFlow.TitleScene;
    [SerializeField] private MonsterDatabase monsterDatabase;
    [SerializeField] private ScreenFader fader;
    [SerializeField] private DialogueRunner dialogue;
    [SerializeField] private ToastView toast;
    [SerializeField] private AudioService audioService;
    [SerializeField] private PauseMenu pauseMenu;

    [Tooltip("Tugas P2: pakai DisabledMoveInput, Player diam tanpa mengubah PlayerMovement.")]
    [SerializeField] private bool disableMovement;

    private GameServices services;

    public GameServices Services => services;

    void Awake()
    {
        Application.targetFrameRate = 60;

        IMoveInput move = disableMovement ? new DisabledMoveInput() : new InputSystemMoveInput();
        var gate = new InputGate(move, new InputSystemActionInput());

        services = new GameServices
        {
            Input = gate,
            Database = monsterDatabase,
            Party = new PlayerParty(),
            Battle = new BattleSession(),
            Flags = new StoryFlags(),
            Save = new SaveService(),
            Dialogue = dialogue,
            Toast = toast,
            Audio = audioService,
        };
        services.Scenes = new SceneFlow(this, fader, services);

        dialogue.Construct(gate, audioService);
        TextMenu.Sfx = audioService;
        if (pauseMenu != null) pauseMenu.Construct(services);
    }

    void Start() => services.Scenes.GoTo(firstScene);

    void Update()
    {
        // P7: waktu bermain bertambah saat InGame (dunia + battle), tidak saat jeda.
        if (services.Scenes.InGame && (pauseMenu == null || !pauseMenu.IsOpen))
            services.Party.AddPlaytime(Time.unscaledDeltaTime);

        if (MenuInput.Mute)
        {
            bool muted = services.Audio.ToggleMute();
            services.Toast.Show(muted ? "Suara dimatikan (M)" : "Suara dinyalakan (M)");
        }
    }
}
