using System.Collections.Generic;
using UnityEngine;

/// <summary>Daftar 12 MonsterData (P4). Save menyimpan id, lalu GetById mengembalikan asetnya.</summary>
[CreateAssetMenu(fileName = "MonsterDatabase", menuName = "Pulau Monster/Monster Database")]
public class MonsterDatabase : ScriptableObject
{
    [SerializeField] private List<MonsterData> monsters = new List<MonsterData>();

    public IReadOnlyList<MonsterData> All => monsters;

    public MonsterData GetById(string id)
    {
        foreach (MonsterData m in monsters)
            if (m != null && m.Id == id) return m;
        return null;
    }

    public MonsterData GetByName(string displayName)
    {
        foreach (MonsterData m in monsters)
            if (m != null && m.DisplayName == displayName) return m;
        return null;
    }
}
