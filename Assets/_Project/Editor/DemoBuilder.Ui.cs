using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Helper UI: semua koordinat memakai piksel desain demo 320 x 180 (x, y dari kiri-atas), dikali 4.
/// panel(x, y, w, h) -> rect (x-1, y-1, w+2, h+2) karena tepi gelap 1 px di luar.
/// </summary>
public static partial class DemoBuilder
{
    static Font fontRegular;
    static Font fontBold;
    static Sprite spNavy, spGold, spNarr, spPixel, spRing, spArrowDown, spStar;

    static void LoadUiAssets()
    {
        fontRegular = Load<Font>(ArtDir + "/Fonts/PixelifySans-Regular.ttf");
        fontBold = Load<Font>(ArtDir + "/Fonts/PixelifySans-Bold.ttf");
        spNavy = LoadSprite(ArtDir + "/UI/UI_Panel_Navy.png");
        spGold = LoadSprite(ArtDir + "/UI/UI_Panel_Gold.png");
        spNarr = LoadSprite(ArtDir + "/UI/UI_Panel_Narr.png");
        spPixel = LoadSprite(ArtDir + "/UI/UI_Pixel.png");
        spRing = LoadSprite(ArtDir + "/UI/UI_Ring.png");
        spArrowDown = LoadSprite(ArtDir + "/UI/UI_Arrow_Down.png");
        spStar = LoadSprite(ArtDir + "/UI/UI_Icon_Star.png");
    }

    static Canvas NewCanvas(string name, int order, Transform parent = null)
    {
        var go = new GameObject(name, typeof(RectTransform));
        if (parent != null) go.transform.SetParent(parent, false);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = order;
        canvas.pixelPerfect = true;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;   // 1280 x 720, Match 0,5
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static RectTransform Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    static RectTransform StretchGroup(string name, Transform parent) => Stretch(NewRect(name, parent));

    /// <summary>Asal koordinat desain parent (jumlah posisi rect kiri-atas sampai ke Canvas).</summary>
    static Vector2 DesignOrigin(Transform parent)
    {
        Vector2 origin = Vector2.zero;
        for (Transform t = parent; t != null && t.GetComponent<Canvas>() == null; t = t.parent)
        {
            var rt = t as RectTransform;
            if (rt == null) break;
            bool topLeft = rt.anchorMin == new Vector2(0f, 1f) && rt.anchorMax == new Vector2(0f, 1f);
            if (topLeft) origin += new Vector2(rt.anchoredPosition.x / UiTheme.Px, -rt.anchoredPosition.y / UiTheme.Px);
        }
        return origin;
    }

    /// <summary>Tempatkan rect dengan koordinat desain ABSOLUT (layar 320 x 180).</summary>
    static RectTransform PlaceAbs(RectTransform rt, float x, float y, float w, float h)
    {
        Vector2 o = DesignOrigin(rt.parent);
        UiTheme.Place(rt, x - o.x, y - o.y, w, h);
        return rt;
    }

    static Image NewImage(string name, Transform parent, float x, float y, float w, float h, Sprite sprite, Color color)
    {
        RectTransform rt = PlaceAbs(NewRect(name, parent), x, y, w, h);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static Image NewFullImage(string name, Transform parent, Sprite sprite, Color color)
    {
        RectTransform rt = Stretch(NewRect(name, parent));
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    /// <summary>Panel navy/emas/narasi: Image Sliced + Pixels Per Unit Multiplier 1,5625 (1 piksel art = 4 unit kanvas).</summary>
    static Image NewPanel(string name, Transform parent, float x, float y, float w, float h, Sprite sprite)
    {
        Image img = NewImage(name, parent, x - 1f, y - 1f, w + 2f, h + 2f, sprite, Color.white);
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 1.5625f;
        return img;
    }

    static Text NewText(string name, Transform parent, float x, float y, float w, float h, string content, int px,
                        Color color, TextAnchor anchor, bool bold = false, bool shadow = true)
    {
        RectTransform rt = PlaceAbs(NewRect(name, parent), x, y, w, h);
        var text = rt.gameObject.AddComponent<Text>();
        text.font = bold ? fontBold : fontRegular;
        text.fontSize = px * 4;                 // teks 8 px -> fontSize 32
        text.color = color;
        text.alignment = anchor;
        text.text = content;
        text.supportRichText = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        if (shadow)
        {
            var s = rt.gameObject.AddComponent<Shadow>();   // bayangan #120F26 (2, -2)
            s.effectColor = UiTheme.Shadow;
            s.effectDistance = new Vector2(2f, -2f);
        }
        return text;
    }

    /// <summary>Bar = latar #0B0918 + anak Fill (Image Filled, Horizontal, inset 1 px). Mengembalikan Fill.</summary>
    static Image NewBar(string name, Transform parent, float x, float y, float w, float h, Color fill)
    {
        Image back = NewImage(name, parent, x, y, w, h, spPixel, UiTheme.BarBack);
        Image img = NewImage("Fill", back.transform, x + 1f, y + 1f, w - 2f, h - 2f, spPixel, fill);
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillOrigin = (int)Image.OriginHorizontal.Left;
        img.fillAmount = 1f;
        return img;
    }

    /// <summary>TextMenu: tiap baris = Text (Raycast Target aktif) + Button (Transition None, Navigation None).</summary>
    static TextMenu NewMenu(string name, Transform parent, Vector2[] rows, float rowW, float rowH, int px, int columns)
    {
        RectTransform container = StretchGroup(name, parent);
        var menu = container.gameObject.AddComponent<TextMenu>();
        var texts = new Text[rows.Length];
        for (int i = 0; i < rows.Length; i++)
        {
            Text row = NewText($"Row_{i}", container, rows[i].x, rows[i].y, rowW, rowH, $"Pilihan {i}", px,
                UiTheme.Text, TextAnchor.MiddleLeft);
            row.raycastTarget = true;
            var button = row.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            texts[i] = row;
        }
        Set(menu, "rows", texts);
        Set(menu, "columns", columns);
        return menu;
    }

    static Button NewClickCatcher(string name, Transform parent)
    {
        Image img = NewFullImage(name, parent, null, new Color(0f, 0f, 0f, 0f));
        img.raycastTarget = true;
        var button = img.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        return button;
    }
}
