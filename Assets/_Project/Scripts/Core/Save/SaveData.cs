using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Isi save_0.json (P6). Partner disimpan sebagai id, bukan aset.</summary>
[Serializable]
public class SaveData
{
    public int version = SaveService.CurrentVersion;
    public string savedAt;
    public string scene = "Scn_World";
    public Vector2 playerPosition;
    public string partnerId;          // id, bukan aset
    public int partnerLevel, partnerExp, partnerHp;
    public int potions;
    public float playtime;            // layar DEMO SELESAI (P7)
    public List<string> flags = new List<string>();
}
