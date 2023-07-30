using System.Text.Json.Serialization;

namespace Img2mc.Shared;

[Serializable]
public class MinecraftBlock
{
    [JsonInclude]
    public string blockName = "";
    [JsonInclude]
    public TextureMetadata[] textures;
    [JsonInclude]
    public bool exclude = false;

    public string FirstTexture => textures[0].fileName;

    public MinecraftBlock()
    {
        textures = Array.Empty<TextureMetadata>();
    }

    public MinecraftBlock(string blockName, TextureMetadata[] textures)
    {
        this.blockName = blockName;
        this.textures = textures;
    }
}

//should set up default pallets. something like 'only concrete' and stuff.
//disable/enable entire sets of blocks at once maybe at some point.
[Serializable]
public class TextureMetadata
{
    [JsonInclude]
    public string fileName = "";
    [JsonInclude]
    public RGB averageRGB;
    [JsonInclude]
    public float hueVariance;
    [JsonInclude]
    public float saturationVariance;
    [JsonInclude]
    public float averageSaturation;
    [JsonInclude]
    public float valueVariance;
    //whether to disallow using this block in the resulting pixelart.
    //useful for user fine-tuning. (no netherite blocks, no respawn anchors, etc.)
    [JsonInclude]
    public bool exclude = false;

    public TextureMetadata(string fileName, RGB avgRGB, float hueVariance, float saturationVariance, float averageSaturation, float valueVariance)
    {
        this.fileName = fileName;
        //blockName = RemoveFaceName(GetNaiveBlockNameFromFile(fileName));
        averageRGB = avgRGB;
        this.hueVariance = hueVariance;
        this.saturationVariance = saturationVariance;
        this.averageSaturation = averageSaturation;
        this.valueVariance = valueVariance;
    }

    public TextureMetadata() { }
}

[Serializable]
public struct RGB
{
    [JsonInclude]
    public byte r, g, b;

    public RGB(byte r, byte g, byte b)
    {
        this.r = r; 
        this.g = g; 
        this.b = b;
    }

    public readonly override string ToString()
    {
        return $"RGB({r},{g},{b})";
    }

    public readonly string AsHexString()
    {
        return $"#{Convert.ToHexString(new byte[] { r, g, b })}";
    }

    public readonly int RGBDistance(RGB other)
    {
        return Math.Abs((int)r - (int)other.r) + Math.Abs((int)g - (int)other.g) + Math.Abs((int)b - (int)other.b);
    }
}
