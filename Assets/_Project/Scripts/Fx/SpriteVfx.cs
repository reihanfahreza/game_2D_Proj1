using UnityEngine;

/// <summary>Animasi sprite sekali jalan (tebasan 4 frame), lalu kembali ke VfxPool.</summary>
public class SpriteVfx : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float frameTime = 0.05f;

    private VfxPool owner;
    private float time;

    public void Init(VfxPool pool) => owner = pool;

    public void Play()
    {
        time = 0f;
        if (frames.Length > 0) sr.sprite = frames[0];
    }

    void Update()
    {
        time += Time.unscaledDeltaTime;
        int frame = Mathf.FloorToInt(time / frameTime);
        if (frame >= frames.Length)
        {
            if (owner != null) owner.Release(this);
            else gameObject.SetActive(false);
            return;
        }
        sr.sprite = frames[frame];
    }
}
