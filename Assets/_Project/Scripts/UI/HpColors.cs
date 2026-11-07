using UnityEngine;

/// <summary>Warna bar HP: hijau &gt; 50%, emas &gt; 20%, merah selebihnya.</summary>
public static class HpColors
{
    public static readonly Color High = UiTheme.Hex("#5BD16A");
    public static readonly Color Mid = UiTheme.Hex("#FFC61A");
    public static readonly Color Low = UiTheme.Hex("#F0524D");

    public static Color For(float fraction) => fraction > 0.5f ? High : fraction > 0.2f ? Mid : Low;
}
