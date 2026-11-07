using UnityEngine;

/// <summary>Baris dialog sederhana (P6). Dipakai Npc versi dasar (Sari: tips elemen).</summary>
[CreateAssetMenu(fileName = "Dlg_", menuName = "Pulau Monster/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [SerializeField, TextArea(2, 4)] private string[] lines;

    public string[] Lines => lines;
}
