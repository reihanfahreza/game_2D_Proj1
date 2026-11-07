using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Penyadap audio untuk DemoVideoRecorder. Dipasang di objek yang memiliki AudioListener,
/// sehingga OnAudioFilterRead menerima hasil campuran akhir (musik + SFX).
/// </summary>
public class DemoAudioTap : MonoBehaviour
{
    public volatile bool active;
    public readonly List<float> samples = new List<float>(1 << 22);
    public int channels = 2;

    void OnAudioFilterRead(float[] data, int ch)
    {
        channels = ch;
        if (!active) return;
        lock (samples) samples.AddRange(data);
    }
}
