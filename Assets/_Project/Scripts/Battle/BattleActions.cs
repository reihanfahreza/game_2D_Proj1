using System.Collections;
using UnityEngine;

/// <summary>Serang: power 14, meleset 6%.</summary>
public class AttackAction : BattleAction
{
    public virtual int Power => 14;

    /// <summary>null = Serang biasa.</summary>
    protected virtual string Move(MonsterInstance user) => null;

    protected virtual float ElementBonus(MonsterInstance user, MonsterInstance target) => 1f;

    public override IEnumerator Execute(BattleSystem battle, MonsterInstance user, MonsterInstance target)
    {
        string who = (user == battle.Enemy && !battle.IsBoss ? "Lawan " : "") + user.Data.DisplayName;
        string move = Move(user);
        yield return battle.Say(move == null ? $"{who} menyerang!" : $"{who} memakai {move}!", 0.7f);

        battle.RaiseAttacked(user);                 // animasi menerjang
        yield return new WaitForSeconds(0.2f);

        if (Random.value < 0.06f)
        {
            battle.RaiseMissed();
            yield return battle.Say("Serangan meleset!", 0.8f);
            yield break;
        }

        float bonus = ElementBonus(user, target);
        int damage = DamageFormula.Calculate(user, target, Power, bonus * battle.DamageScale(user), out bool critical);
        target.TakeDamage(damage);
        battle.RaiseHit(target, damage, critical, move != null ? user.Data.Element : (Element?)null);

        yield return new WaitForSeconds(0.45f);
        if (critical) yield return battle.Say("Serangan kritis!", 0.7f);
        if (bonus > 1f)
        {
            battle.RaiseSuperEffective(user);
            yield return battle.Say("Sangat efektif!", 0.7f);
        }
        else if (bonus < 1f) yield return battle.Say("Kurang efektif...", 0.7f);
    }
}

/// <summary>Jurus elemen: power 22 x elemen, 3x per battle (lawan maks 2x).</summary>
public class SkillAction : AttackAction
{
    public override int Power => 22;

    protected override string Move(MonsterInstance user) => user.Data.SkillName;

    protected override float ElementBonus(MonsterInstance user, MonsterInstance target) =>
        ElementChart.Multiplier(user.Data.Element, target.Data.Element);
}

/// <summary>Ramuan: pulih 40% MaxHP, Priority 1.</summary>
public class PotionAction : BattleAction
{
    public override int Priority => 1;

    public override IEnumerator Execute(BattleSystem battle, MonsterInstance user, MonsterInstance target)
    {
        battle.Party.UsePotion();
        int healed = user.Heal(Mathf.RoundToInt(user.MaxHP * 0.4f));
        battle.RaiseHealed(user);
        yield return battle.Say($"Kamu memakai Ramuan. {user.Data.DisplayName} pulih {healed} HP.", 0.8f);
    }
}

/// <summary>Kabur: 70%, Priority 2. Boss: tidak bisa (giliran tidak hilang).</summary>
public class RunAction : BattleAction
{
    public override int Priority => 2;

    public override IEnumerator Execute(BattleSystem battle, MonsterInstance user, MonsterInstance target)
    {
        if (battle.IsBoss)
        {
            yield return battle.Say($"Tidak bisa kabur dari {target.Data.DisplayName}!", 0.8f);
            yield break;
        }
        if (Random.value < 0.7f)
        {
            yield return battle.Say("Berhasil kabur!", 0.7f);
            battle.Escape();
        }
        else
        {
            yield return battle.Say("Gagal kabur!", 0.7f);
        }
    }
}
