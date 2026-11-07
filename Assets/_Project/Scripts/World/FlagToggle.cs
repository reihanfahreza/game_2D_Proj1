using UnityEngine;

/// <summary>Menyembunyikan/memunculkan objek sesuai flag cerita (P6). Contoh: palang Gate_Hutan hilang saat has_starter.</summary>
public class FlagToggle : MonoBehaviour
{
    [SerializeField] private string flagKey = StoryFlags.HasStarter;
    [SerializeField] private GameObject hideWhenSet;
    [SerializeField] private GameObject showWhenSet;

    private StoryFlags flags;

    public void Construct(StoryFlags storyFlags)
    {
        flags = storyFlags;
        flags.Changed += OnFlag;
        Apply();
    }

    void OnDestroy()
    {
        if (flags != null) flags.Changed -= OnFlag;
    }

    private void OnFlag(string key) => Apply();

    private void Apply()
    {
        bool set = flags.Has(flagKey);
        if (hideWhenSet != null) hideWhenSet.SetActive(!set);
        if (showWhenSet != null) showWhenSet.SetActive(set);
    }
}
