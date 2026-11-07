using System.Collections;
using UnityEngine;

/// <summary>
/// Monster liar dengan FSM (P4). Bersentuhan dengan Player -> BattleSession.Begin -> SceneFlow.StartBattle.
/// Tenang (tidak mengejar) saat cooldown 2,5 s setelah kalah/kabur, 2,2 s waktu aman setelah battle,
/// atau saat input terkunci (dialog, P6). Menang -> hilang lalu muncul lagi 20 s kemudian.
/// Boss Raijugar: stationary, mengaum lewat DialogueRunner (P6) sebelum battle.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class WildMonster : MonoBehaviour
{
    [SerializeField] private MonsterData species;
    [SerializeField] private int minLevel = 2;
    [SerializeField] private int maxLevel = 4;
    [SerializeField] private bool isBoss;
    [SerializeField] private bool stationary;
    [SerializeField] private float sightRadius = 3.25f;
    [SerializeField] private float giveUpRadius = 6.25f;
    [SerializeField] private float wanderSpeed = 1.125f;
    [SerializeField] private float chaseSpeed = 2.5f;
    [SerializeField] private float wanderRadius = 3f;
    [SerializeField] private float respawnSeconds = 20f;
    [SerializeField] private float cooldownSeconds = 2.5f;
    [SerializeField] private int fleeLevelGap = 5;
    [SerializeField] private GameObject alert;            // sprite UI_Alert, y +1,3 (boss +2,2)
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private Collider2D bodyCollider;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject[] bossExtras;     // BossLight: ikut hilang setelah kalah

    private BattleSession session;
    private SceneFlow scenes;
    private PlayerParty party;
    private StoryFlags flags;
    private InputGate gate;
    private DialogueRunner dialogue;

    private StateMachine fsm;
    private Vector2 desiredVelocity;
    private float cooldown;
    private float respawnTimer;
    private bool inBattle;
    private bool hidden;
    private int level;

    public WanderState Wander { get; private set; }
    public ChaseState Chase { get; private set; }
    public ReturnState Return { get; private set; }
    public FleeState Flee { get; private set; }

    public Transform Target { get; private set; }
    public Vector2 Home { get; private set; }
    public Vector2 Position => rb.position;
    public float WanderSpeed => wanderSpeed;
    public float ChaseSpeed => chaseSpeed;
    public float WanderRadius => wanderRadius;
    public MonsterData Species => species;
    public bool IsBoss => isBoss;
    public bool IsHidden => hidden;
    public int Level => level;

    void Awake()
    {
        Home = transform.position;
        fsm = new StateMachine();
        Wander = new WanderState(this);
        Chase = new ChaseState(this);
        Return = new ReturnState(this);
        Flee = new FleeState(this);
        fsm.ChangeTo(Wander);
        if (alert != null) alert.SetActive(false);
        if (animator != null && species != null && species.IdleController != null)
            animator.runtimeAnimatorController = species.IdleController;
        RollLevel();
    }

    public void Construct(BattleSession battleSession, SceneFlow sceneFlow, PlayerParty playerParty, Transform target,
                          StoryFlags storyFlags, InputGate inputGate, DialogueRunner dialogueRunner)
    {
        session = battleSession;
        scenes = sceneFlow;
        party = playerParty;
        Target = target;
        flags = storyFlags;
        gate = inputGate;
        dialogue = dialogueRunner;

        if (isBoss && flags != null && flags.Has(StoryFlags.BossDefeated)) Hide(true);
    }

    private bool Calm => cooldown > 0f || (session != null && session.IsSafe) || (gate != null && gate.IsLocked);

    public float DistanceToTarget => Target == null ? float.MaxValue : Vector2.Distance(rb.position, Target.position);

    public bool CanSeeTarget => party != null && party.Partner != null && !Calm && DistanceToTarget < sightRadius;

    public bool LostTarget => Calm || DistanceToTarget > giveUpRadius;

    public bool ShouldFlee => party != null && party.Partner != null && party.Partner.Level >= level + fleeLevelGap;

    void Update()
    {
        if (cooldown > 0f) cooldown -= Time.deltaTime;
        if (hidden)
        {
            TickRespawn();
            return;
        }

        if (stationary || inBattle || Target == null) desiredVelocity = Vector2.zero;
        else fsm.Tick(Time.deltaTime);

        if (alert != null) alert.SetActive(fsm.Current == Chase && !inBattle);

        // Tantangan P4: berkedip selama waktu aman 2,2 detik berjalan
        bool blink = session != null && session.IsSafe && !inBattle && Mathf.FloorToInt(Time.time * 10f) % 2 == 0;
        body.enabled = !blink;

        if (Mathf.Abs(desiredVelocity.x) > 0.05f) body.flipX = desiredVelocity.x > 0f;
    }

    void FixedUpdate()
    {
        if (!hidden) rb.linearVelocity = desiredVelocity;
    }

    public void ChangeState(IState next) => fsm.ChangeTo(next);

    public void MoveTowards(Vector2 point, float speed)
    {
        Vector2 d = point - rb.position;
        desiredVelocity = d.sqrMagnitude < 0.0025f ? Vector2.zero : d.normalized * speed;
    }

    public void MoveAwayFrom(Vector2 point, float speed)
    {
        Vector2 d = rb.position - point;
        desiredVelocity = d.sqrMagnitude < 0.0001f ? Vector2.right * speed : d.normalized * speed;
    }

    public void Stop() => desiredVelocity = Vector2.zero;

    void OnCollisionEnter2D(Collision2D c) => TryStartBattle(c.collider);

    // Sentuh terus-menerus juga memicu battle (boss diam di tempat)
    void OnCollisionStay2D(Collision2D c)
    {
        if (isBoss) TryStartBattle(c.collider);
    }

    private void TryStartBattle(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        StartEncounter();
    }

    /// <summary>Mulai pertemuan (dipakai tabrakan dan DemoAutoPilot).</summary>
    public void StartEncounter()
    {
        if (inBattle || hidden || session == null || scenes == null) return;
        if (party == null || party.Partner == null) return;
        if (Calm || scenes.IsBusy) return;

        if (isBoss && dialogue != null) StartCoroutine(BossEncounter());
        else BeginBattle();
    }

    private IEnumerator BossEncounter()
    {
        inBattle = true;
        dialogue.Run(Roar());
        yield return null;
        while (dialogue.IsRunning) yield return null;
        BeginBattle();
    }

    private IEnumerator Roar()
    {
        yield return dialogue.Say(species.DisplayName, "GRRRAAAH!!");
        yield return dialogue.Say("", $"{species.DisplayName}, penguasa Gua Petir, menghalangi jalan!");
    }

    private void BeginBattle()
    {
        inBattle = true;
        desiredVelocity = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
        session.Begin(new MonsterInstance(species, level), isBoss, OnBattleFinished);
        scenes.StartBattle();
    }

    private void OnBattleFinished(BattleResult result)
    {
        inBattle = false;
        if (result == BattleResult.Win)
        {
            if (isBoss) flags?.Set(StoryFlags.BossDefeated);
            Hide(isBoss);
        }
        else
        {
            cooldown = cooldownSeconds;   // kalah / kabur
            fsm.ChangeTo(Return);
        }
    }

    private void Hide(bool permanent)
    {
        hidden = true;
        desiredVelocity = Vector2.zero;
        body.enabled = false;
        bodyCollider.enabled = false;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        if (alert != null) alert.SetActive(false);
        if (bossExtras != null)
            foreach (GameObject extra in bossExtras)
                if (extra != null) extra.SetActive(false);
        respawnTimer = permanent ? -1f : respawnSeconds;
    }

    private void TickRespawn()
    {
        if (respawnTimer < 0f) return;
        respawnTimer -= Time.deltaTime;
        if (respawnTimer > 0f) return;
        if (Target != null && Vector2.Distance(Home, Target.position) < 2f)
        {
            respawnTimer = 1f;     // tunggu pemain menjauh
            return;
        }

        rb.position = Home;
        transform.position = Home;
        hidden = false;
        body.enabled = true;
        bodyCollider.enabled = true;
        rb.simulated = true;
        RollLevel();
        fsm.ChangeTo(Wander);
    }

    private void RollLevel() => level = Random.Range(minLevel, maxLevel + 1);

    void OnDrawGizmosSelected()
    {
        Vector3 home = Application.isPlaying ? (Vector3)Home : transform.position;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(home, wanderRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, sightRadius);
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, giveUpRadius);
    }
}
