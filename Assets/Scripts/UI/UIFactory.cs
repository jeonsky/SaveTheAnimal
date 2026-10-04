using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 런타임 UI 생성 헬퍼.
/// 별도 이미지 리소스 없이 둥근 사각형/하트 스프라이트를 코드로 만들어 쓴다.
/// </summary>
public static class UIFactory
{
    // ───────────────────────── 컬러 팔레트 ─────────────────────────
    public static readonly Color Dim    = new Color(0.10f, 0.08f, 0.12f, 0.70f);
    public static readonly Color Paper  = new Color(1f, 0.95f, 0.88f, 0.94f);
    public static readonly Color Cream  = Hex("FFF8EE");
    public static readonly Color Ink    = Hex("3D3A36");
    public static readonly Color SubInk = Hex("8A8178");
    public static readonly Color Green  = Hex("4CB872");
    public static readonly Color Orange = Hex("FF9F43");
    public static readonly Color Red    = Hex("FF5A6E");
    public static readonly Color Blue   = Hex("4E9BE0");
    public static readonly Color Gray   = Hex("A79E94");
    public static readonly Color Track  = Hex("E9DFD3");

    public static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }

    // ───────────────────────── 코드로 만드는 스프라이트 ─────────────────────────
    static Sprite rounded;
    static Sprite heart;

    public static Sprite Rounded
    {
        get
        {
            if (rounded == null) rounded = CreateRoundedSprite(64, 22);
            return rounded;
        }
    }

    public static Sprite Heart
    {
        get
        {
            if (heart == null) heart = CreateHeartSprite(128);
            return heart;
        }
    }

    static Texture2D NewTexture(int size)
    {
        return new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.DontUnloadUnusedAsset,
        };
    }

    static Sprite CreateRoundedSprite(int size, int radius)
    {
        Texture2D tex = NewTexture(size);
        var pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                var c = new Vector2(Mathf.Clamp(p.x, radius, size - radius), Mathf.Clamp(p.y, radius, size - radius));
                float alpha = Mathf.Clamp01(radius - Vector2.Distance(p, c) + 0.5f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        Sprite s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        s.hideFlags = HideFlags.DontUnloadUnusedAsset;
        return s;
    }

    // 하트 곡선 (x²+y²-1)³ - x²y³ ≤ 0 을 4x4 슈퍼샘플링으로 그림
    static Sprite CreateHeartSprite(int size)
    {
        Texture2D tex = NewTexture(size);
        var pixels = new Color32[size * size];
        const int ss = 4;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int hit = 0;
                for (int sy = 0; sy < ss; sy++)
                {
                    for (int sx = 0; sx < ss; sx++)
                    {
                        float u = ((x + (sx + 0.5f) / ss) / size - 0.5f) * 2.6f;
                        float v = ((y + (sy + 0.5f) / ss) / size - 0.5f) * 2.6f + 0.12f;
                        float a = u * u + v * v - 1f;
                        if (a * a * a - u * u * v * v * v <= 0f) hit++;
                    }
                }
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(255 * hit / (ss * ss)));
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        Sprite s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        s.hideFlags = HideFlags.DontUnloadUnusedAsset;
        return s;
    }

    // ───────────────────────── 레이아웃 헬퍼 ─────────────────────────

    public static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    public static RectTransform RT(this Component c) => (RectTransform)c.transform;

    /// <summary>부모 전체를 채움 (여백 지정 가능)</summary>
    public static RectTransform Stretch(this RectTransform rt, float left = 0, float bottom = 0, float right = 0, float top = 0)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
        return rt;
    }

    /// <summary>anchor 지점 기준으로 pos 위치, size 크기로 배치 (pivot = anchor)</summary>
    public static RectTransform Place(this RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    /// <summary>부모 중앙 기준 배치</summary>
    public static RectTransform Center(this RectTransform rt, float x, float y, float w, float h)
    {
        return rt.Place(new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(w, h));
    }

    /// <summary>부모 상단에 가로로 꽉 차는 띠 (top = 위에서부터 거리)</summary>
    public static RectTransform TopStrip(this RectTransform rt, float top, float height, float left = 0, float right = 0)
    {
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.offsetMin = new Vector2(left, -top - height);
        rt.offsetMax = new Vector2(-right, -top);
        return rt;
    }

    // ───────────────────────── 위젯 ─────────────────────────

    public static Image Img(Transform parent, string name, Color color, Sprite sprite = null)
    {
        RectTransform rt = CreateRect(name, parent);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;

        if (sprite != null && sprite.border != Vector4.zero)
        {
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 0.6f; // 모서리 더 둥글게
        }
        return img;
    }

    public static TextMeshProUGUI Label(Transform parent, string text, float size, Color color,
        bool bold = false, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        RectTransform rt = CreateRect("Label", parent);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        return t;
    }

    public static Button MakeButton(Transform parent, string label, Color color, Action onClick, float fontSize = 56)
    {
        Image img = Img(parent, "Button_" + label, color, Rounded);
        img.raycastTarget = true;

        var btn = img.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;

        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.96f, 0.96f, 0.96f);
        cb.pressedColor = new Color(0.82f, 0.82f, 0.82f);
        cb.selectedColor = Color.white;
        cb.disabledColor = new Color(0.75f, 0.75f, 0.75f, 0.5f);
        cb.fadeDuration = 0.08f;
        btn.colors = cb;

        if (!string.IsNullOrEmpty(label))
        {
            TextMeshProUGUI t = Label(img.transform, label, fontSize, Color.white, true);
            t.rectTransform.Stretch();
        }

        btn.onClick.AddListener(() => onClick?.Invoke());
        img.gameObject.AddComponent<ButtonPop>();
        return btn;
    }

    public class Bar
    {
        public RectTransform Root;
        public Image Fill;
        public TextMeshProUGUI Text;

        public void Set(float ratio)
        {
            Fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
        }
    }

    public static Bar MakeBar(Transform parent, string name, Color fillColor, float fontSize = 36)
    {
        Image bg = Img(parent, name, Track, Rounded);

        Image fill = Img(bg.transform, "Fill", fillColor, Rounded);
        fill.rectTransform.anchorMin = Vector2.zero;
        fill.rectTransform.anchorMax = Vector2.one;
        fill.rectTransform.offsetMin = Vector2.zero;
        fill.rectTransform.offsetMax = Vector2.zero;

        TextMeshProUGUI t = Label(bg.transform, "", fontSize, Ink, true);
        t.rectTransform.Stretch();

        return new Bar { Root = bg.rectTransform, Fill = fill, Text = t };
    }
}
