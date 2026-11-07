using UnityEngine;

/// <summary>
/// Satu tempat untuk warna UI navy-emas (P5) + konversi koordinat desain.
/// Koordinat desain = layar demo 320 x 180 piksel, dikali 4 (Canvas Scaler 1280 x 720).
/// </summary>
public static class UiTheme
{
    public static readonly Color Ink = Hex("#121027");
    public static readonly Color Panel = Hex("#1C1540");
    public static readonly Color Gold = Hex("#FFC61A");
    public static readonly Color Text = Hex("#EDE9F7");
    public static readonly Color Muted = Hex("#A9A3C9");
    public static readonly Color Disabled = Hex("#5C5680");
    public static readonly Color Exp = Hex("#6F9BFF");
    public static readonly Color BarBack = Hex("#0B0918");
    public static readonly Color Shadow = Hex("#120F26");
    public static readonly Color Cream = Hex("#F6E7C1");

    /// <summary>1 piksel demo = 4 unit kanvas.</summary>
    public const float Px = 4f;

    public static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }

    /// <summary>x, y dari kiri-atas (piksel demo) -> anchor &amp; pivot (0, 1), anchoredPosition (x*4, -y*4), sizeDelta (w*4, h*4).</summary>
    public static void Place(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x * Px, -y * Px);
        rt.sizeDelta = new Vector2(w * Px, h * Px);
    }
}
