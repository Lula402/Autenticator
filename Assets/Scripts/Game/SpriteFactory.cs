using UnityEngine;

// Genera sprites simples por código para no depender de imágenes externas.
public static class SpriteFactory
{
    private const int Size = 64;

    private static Sprite _circle;
    private static Sprite _roundedBox;
    private static Sprite _square;

    public static Sprite Square
    {
        get
        {
            if (_square == null) _square = Create((x, y) => 1f);
            return _square;
        }
    }

    public static Sprite Circle
    {
        get
        {
            if (_circle == null) _circle = Create(CircleAlpha);
            return _circle;
        }
    }

    public static Sprite RoundedBox
    {
        get
        {
            if (_roundedBox == null) _roundedBox = Create(RoundedBoxAlpha);
            return _roundedBox;
        }
    }

    private static Sprite Create(System.Func<float, float, float> alphaAt)
    {
        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        var pixels = new Color32[Size * Size];
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                float alpha = Mathf.Clamp01(alphaAt(x + 0.5f, y + 0.5f));
                pixels[y * Size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
    }

    private static float CircleAlpha(float x, float y)
    {
        float radius = Size / 2f;
        float distance = Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius));
        return radius - distance;
    }

    private static float RoundedBoxAlpha(float x, float y)
    {
        const float cornerRadius = 16f;
        float half = Size / 2f;
        float dx = Mathf.Max(Mathf.Abs(x - half) - (half - cornerRadius), 0f);
        float dy = Mathf.Max(Mathf.Abs(y - half) - (half - cornerRadius), 0f);
        return cornerRadius - Mathf.Sqrt(dx * dx + dy * dy);
    }
}
