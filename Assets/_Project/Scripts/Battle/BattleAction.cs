using System.Collections;

/// <summary>
/// Inheritance &amp; polymorphism (P5): Serang / Jurus / Ramuan / Kabur turunan satu class abstrak.
/// Priority &gt; 0 selalu jalan lebih dulu (Ramuan 1, Kabur 2).
/// </summary>
public abstract class BattleAction
{
    public virtual int Priority => 0;

    public abstract IEnumerator Execute(BattleSystem battle, MonsterInstance user, MonsterInstance target);
}
