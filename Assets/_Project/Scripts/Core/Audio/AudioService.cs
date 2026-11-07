using System.Collections;
using UnityEngine;

/// <summary>
/// Musik dua AudioSource dengan crossfade 1 detik (unscaled) + satu AudioSource SFX (P6).
/// AudioListener tunggal ada di objek ini (Scn_Bootstrap), jadi kamera scene tidak perlu listener.
/// </summary>
public class AudioService : MonoBehaviour
{
    [SerializeField] private AudioSource musicA;
    [SerializeField] private AudioSource musicB;
    [SerializeField] private AudioSource sfx;
    [SerializeField] private float crossfadeSeconds = 1f;
    [SerializeField] private float musicVolume = 0.5f;

    [Header("Suara umum (menu, dialog, encounter)")]
    [SerializeField] private AudioClip uiMove;
    [SerializeField] private AudioClip uiConfirm;
    [SerializeField] private AudioClip uiCancel;
    [SerializeField] private AudioClip uiError;
    [SerializeField] private AudioClip dialogBlip;
    [SerializeField] private AudioClip encounter;

    private AudioSource current;
    private AudioClip worldMusic;
    private Coroutine fading;

    public bool Muted { get; private set; }

    void Awake()
    {
        current = musicA;
        musicA.loop = true;
        musicB.loop = true;
    }

    public void PlayWorldMusic(AudioClip clip)
    {
        worldMusic = clip;
        PlayMusic(clip);
    }

    public void PlayBattleMusic(AudioClip clip) => PlayMusic(clip);

    /// <summary>Dipanggil SceneFlow setelah battle.</summary>
    public void ResumeWorldMusic() => PlayMusic(worldMusic);

    private void PlayMusic(AudioClip clip)
    {
        if (current.clip == clip && (clip == null || current.isPlaying)) return;

        AudioSource next = current == musicA ? musicB : musicA;
        if (fading != null)
        {
            StopCoroutine(fading);
            next.Stop();
            next.volume = 0f;
        }

        next.clip = clip;
        next.volume = 0f;
        if (clip != null) next.Play();
        fading = StartCoroutine(Crossfade(current, next));
        current = next;
    }

    private IEnumerator Crossfade(AudioSource from, AudioSource to)
    {
        float fromStart = from.volume;
        for (float t = 0f; t < crossfadeSeconds; t += Time.unscaledDeltaTime)
        {
            float k = t / crossfadeSeconds;
            from.volume = Mathf.Lerp(fromStart, 0f, k);
            to.volume = Mathf.Lerp(0f, musicVolume, k);
            yield return null;
        }
        from.volume = 0f;
        from.Stop();
        to.volume = to.clip != null ? musicVolume : 0f;
        fading = null;
    }

    public void PlaySfx(AudioClip clip, float pitchJitter = 0.05f)
    {
        if (clip == null) return;
        sfx.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
        sfx.PlayOneShot(clip);
    }

    public void PlayUiMove() => PlaySfx(uiMove, 0f);
    public void PlayUiConfirm() => PlaySfx(uiConfirm, 0f);
    public void PlayUiCancel() => PlaySfx(uiCancel, 0f);
    public void PlayUiError() => PlaySfx(uiError, 0f);
    public void PlayBlip() => PlaySfx(dialogBlip, 0.08f);
    public void PlayEncounter() => PlaySfx(encounter, 0f);

    /// <summary>Tombol M.</summary>
    public bool ToggleMute()
    {
        Muted = !Muted;
        AudioListener.volume = Muted ? 0f : 1f;
        return Muted;
    }
}
