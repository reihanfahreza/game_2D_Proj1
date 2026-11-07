using System;
using UnityEngine;

/// <summary>
/// Area peta (P3). Box Collider 2D trigger; Player wajib ber-tag Player.
/// P5 menambah event Entered (HUD nama area), P6 menambah musik area.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class AreaZone : MonoBehaviour
{
    [SerializeField] private string areaName = "Desa Awal";
    [SerializeField] private AudioClip music;

    private AudioService audioService;

    public string AreaName => areaName;

    public event Action<AreaZone> Entered;
    public event Action<AreaZone> Exited;

    public void Construct(AudioService audio) => audioService = audio;

    public void PlayMusic()
    {
        if (audioService != null && music != null) audioService.PlayWorldMusic(music);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (Entered == null) Debug.Log($"Masuk area: {areaName}");   // versi P3
        Entered?.Invoke(this);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) Exited?.Invoke(this);
    }
}
