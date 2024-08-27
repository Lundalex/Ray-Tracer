using UnityEngine;
using System.Text;
using TMPro;
using Unity.Mathematics;
public class AsciiHelper : MonoBehaviour
{
    public int2 artResolution;
    public bool DoColor;
    public int BlurRadius;
    public int BlurIterations;
    public TextMeshProUGUI Asciitext;
    public TextureHelper textureHelper;

    private bool ProgramStarted;

    private static readonly char[] asciiChars = { '@', '#', '8', '&', 'o', 'o', 'o'};
    private readonly float2 BaseResolution = new(150, 84);
    private readonly float2 BaseScale = new(1.3f, 0.62f);
    private readonly float2 BasePosition = new(0.0f, -4040f);

    private void Start() => ProgramStarted = true;

    private void OnValidate()
    {
        if (ProgramStarted)
        {
            RectTransform rectTransform = Asciitext.rectTransform;
            rectTransform.localScale = new Vector3(BaseScale.x / (artResolution.x / BaseResolution.x), BaseScale.y / (artResolution.y / BaseResolution.y), 1);
            rectTransform.anchoredPosition = new Vector3(BasePosition.x, BasePosition.y - 6200 * (BaseResolution.y / artResolution.y - 1), 0);
        }
    }

    public Texture2D RenderTextureToTexture2D(RenderTexture rt)
    {
        // Create a new Texture2D to store the data
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);

        // Set the RenderTexture as the active render texture
        RenderTexture.active = rt;
        // Read the pixels from the RenderTexture
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        // Reset the active RenderTexture
        RenderTexture.active = null;

        return tex;
    }

public string ConvertTextureToASCII(Texture2D tex)
{
    StringBuilder sb = new StringBuilder();
    Texture2D resizedTex = ResizeTexture(tex, artResolution.x, artResolution.y);
    Color[] pixels = resizedTex.GetPixels();

    for (int y = 0; y < artResolution.y; y++)
    {
        for (int x = 0; x < artResolution.x; x++)
        {
            int pixelIndex = y * artResolution.x + x;
            Color color = pixels[pixelIndex];
            float brightness = (color.r + color.g + color.b) / 3f;
            int index = Mathf.RoundToInt((1 - brightness) * (asciiChars.Length - 1));
            char asciiChar = asciiChars[index];

            if (DoColor)
            {
                // Convert color to hex code for rich text
                string colorHex = ColorToHex(color);
                sb.Append($"<color=#{colorHex}>{asciiChar}</color>");
            }
            else
            {
                sb.Append(asciiChar);
            }
        }
        sb.Append('\n');
    }

    return sb.ToString();
}

    public Texture2D ResizeTexture(Texture2D tex, int targetWidth, int targetHeight)
    {
        int2 res = new(tex.width, tex.height);
        textureHelper.BoxBlur(ref tex, res, BlurRadius, BlurIterations);

        Texture2D resizedTex = new Texture2D(targetWidth, targetHeight);
        float scaleX = (float)tex.width / targetWidth;
        float scaleY = (float)tex.height / targetHeight;

        for (int y = 0; y < targetHeight; y++)
        {
            for (int x = 0; x < targetWidth; x++)
            {
                // Calculate the corresponding pixel position in the original texture
                int px = Mathf.FloorToInt(x * scaleX);
                int py = Mathf.FloorToInt((targetHeight - y - 1) * scaleY); // Flip Y-axis to correct upside-down issue
                Color color = tex.GetPixel(px, py);
                resizedTex.SetPixel(x, y, color);
            }
        }
        resizedTex.Apply();
        return resizedTex;
    }

    private string ColorToHex(Color color)
    {
        // Convert Color to hex string
        return ColorUtility.ToHtmlStringRGB(color);
    }
}