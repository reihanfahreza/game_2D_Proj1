using UnityEngine;

/// <summary>
/// State monster liar (P4). Tiga state, masing-masing tahu kapan pindah:
/// Wander (jalan acak/diam, 1,125 unit/s, maks 3 unit dari Home) -> Chase bila Player &lt; 3,25
/// Chase (mendekati Player, 2,5 unit/s, tanda !) -> Return bila Player &gt; 6,25
/// Return (pulang ke Home) -> Wander saat sampai
/// Tugas P4: Flee (kabur) bila partner pemain 5 level lebih tinggi.
/// </summary>
public class WanderState : IState
{
    private readonly WildMonster m;
    private Vector2 target;
    private float wait;
    private float walkTime;

    public WanderState(WildMonster monster) => m = monster;

    public void Enter()
    {
        PickTarget();
        wait = Random.Range(0.3f, 1.2f);
    }

    public void Tick(float deltaTime)
    {
        if (m.CanSeeTarget)
        {
            m.ChangeState(m.ShouldFlee ? (IState)m.Flee : m.Chase);
            return;
        }

        if (wait > 0f)
        {
            wait -= deltaTime;
            m.Stop();
            return;
        }

        m.MoveTowards(target, m.WanderSpeed);
        walkTime += deltaTime;
        if (Vector2.Distance(m.Position, target) < 0.1f || walkTime > 3f)
        {
            PickTarget();
            wait = Random.Range(0.6f, 2f);
        }
    }

    public void Exit() => m.Stop();

    private void PickTarget()
    {
        target = m.Home + Random.insideUnitCircle * m.WanderRadius;
        walkTime = 0f;
    }
}

public class ChaseState : IState
{
    private readonly WildMonster m;

    public ChaseState(WildMonster monster) => m = monster;

    public void Enter() { }

    public void Tick(float deltaTime)
    {
        if (m.LostTarget)
        {
            m.ChangeState(m.Return);
            return;
        }
        m.MoveTowards(m.Target.position, m.ChaseSpeed);
    }

    public void Exit() => m.Stop();
}

public class ReturnState : IState
{
    private readonly WildMonster m;
    private float time;

    public ReturnState(WildMonster monster) => m = monster;

    public void Enter() => time = 0f;

    public void Tick(float deltaTime)
    {
        time += deltaTime;
        m.MoveTowards(m.Home, m.WanderSpeed * 1.5f);
        if (Vector2.Distance(m.Position, m.Home) < 0.1f || time > 8f)
            m.ChangeState(m.Wander);
    }

    public void Exit() => m.Stop();
}

/// <summary>Tugas P4: monster kabur bila partner pemain 5 level lebih tinggi.</summary>
public class FleeState : IState
{
    private readonly WildMonster m;

    public FleeState(WildMonster monster) => m = monster;

    public void Enter() { }

    public void Tick(float deltaTime)
    {
        if (m.LostTarget || !m.ShouldFlee)
        {
            m.ChangeState(m.Return);
            return;
        }
        m.MoveAwayFrom(m.Target.position, m.ChaseSpeed);
    }

    public void Exit() => m.Stop();
}
