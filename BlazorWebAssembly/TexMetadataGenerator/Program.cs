// See https://aka.ms/new-console-template for more information
using Img2mc.Shared;
using SixLabors.ImageSharp;
using System.Text.Json;
using TexMetadataGenerator;

enterPath:
Console.WriteLine("Enter directory path for textures:");
string path = Console.ReadLine() ?? "";
while (!Directory.Exists(path))
{
    Console.WriteLine("Invalid Path, try again:");
    path = Console.ReadLine() ?? "";
}

Console.WriteLine($"Selected path: {path}");
if (!GetUserConfirmation())
{
    goto enterPath;
}

var files = Directory.GetFiles(path);

Console.WriteLine($"Found {files.Length} files.");

TextureMetadata[] textures = new TextureMetadata[files.Length];
int i;
for (i = 0; i < files.Length; i++)
{
    textures[i] = ProcessImage(files[i]);
}

Dictionary<string, List<TextureMetadata>> blockTextureDictionary = new();

foreach (var texture in textures)
{
    var blockName = BlockNameHelper.GetBlockName(texture.fileName);
    if (blockTextureDictionary.ContainsKey(blockName))
    {
        blockTextureDictionary[blockName].Add(texture);
    }
    else
    {
        var textureList = new List<TextureMetadata>
        {
            texture
        };
        blockTextureDictionary[blockName] = textureList;
    }
}

Console.WriteLine($"Processed {textures.Length} textures.");
Console.WriteLine($"Found {blockTextureDictionary.Count} unique blocks.");

MinecraftBlock[] blocks = new MinecraftBlock[blockTextureDictionary.Count];
i = 0;
foreach(var pair in blockTextureDictionary)
{
    blocks[i] = new MinecraftBlock(pair.Key, pair.Value.ToArray());
    i++;
}

string outputPath = Path.Combine(path, "__output_metadata.json");
Console.WriteLine($"Ouput to: {outputPath}");

using FileStream fs = File.Create(outputPath);
var options = new JsonSerializerOptions
{
    WriteIndented = GetUserConfirmation("Write as human readable? (Y/N)"),
};
JsonSerializer.Serialize(fs, blocks, options);
fs.Flush();
Console.WriteLine("Done.");

static bool GetUserConfirmation(string? prompt = null)
{
    Console.WriteLine(prompt ?? "Confirm (Y/N):");
    while (true)
    {
        var c = Console.ReadKey();
        if(c.Key == ConsoleKey.Y)
        {
            Console.WriteLine();
            return true;
        }
        if(c.Key == ConsoleKey.N)
        {
            Console.WriteLine();
            return false;
        }
        int cursor = Console.CursorTop;
        Console.SetCursorPosition(0, cursor);
    }
}

static TextureMetadata ProcessImage(string file)
{
    using Image<Rgb24> img = Image.Load<Rgb24>(file);
    int pixelCount = img.Width * img.Height;
    //unwrap the image into a 1d array
    Rgb24[] pixels = new Rgb24[pixelCount];
    for (int x = 0; x < img.Width; x++)
    {
        for (int y = 0; y < img.Height; y++)
        {
            pixels[x * img.Width + y] = img[x, y];
        }
    }
    
    Rgb24 averageRGB = GetAverageColor(img);
    HSVColor averageHSV = HSVColor.FromRGB(averageRGB);
    float hueVariance = 0;
    float saturationVariance = 0;
    float valueVariance = 0;
    float saturationTotal = 0;
    foreach (var pixel in pixels)
    {
        HSVColor currentHSV = HSVColor.FromRGB(pixel);
        hueVariance = Math.Max(hueVariance, GetHueDistance(averageHSV.h, currentHSV.h));
        saturationVariance = Math.Max(saturationVariance, Math.Abs(currentHSV.s - averageHSV.s));
        saturationTotal += currentHSV.s;
        valueVariance = Math.Max(valueVariance, Math.Abs(currentHSV.v - averageHSV.v));
    }

    return new(Path.GetFileName(file),
               new(averageRGB.R, averageRGB.G, averageRGB.B), 
               hueVariance, 
               saturationVariance, 
               saturationTotal / pixelCount, 
               valueVariance);
}

static Rgb24 GetAverageColor(Image<Rgb24> image)
{
    int pixelCount = image.Height * image.Width;
    int r = 0, g = 0, b = 0;
    for (int i = 0; i < image.Height; i++)
    {
        for (int j = 0; j < image.Width; j++)
        {
            var col = image[j, i];
            r += col.R;
            g += col.G;
            b += col.B;
        }
    }
    //int argb = (255 << 24) | ((r / pixelCount) << 16) | ((g / pixelCount) << 8) | (b / pixelCount);
    return new((byte)(r /pixelCount), (byte)(g /pixelCount), (byte)(b/pixelCount));
}

static float GetHueDistance(float h1, float h2)
{
    //both are [0, 360]
    //0, 355 should result in 5
    float diff = h2 - h1;
    if (diff < 0)
    {
        return GetHueDistance(h2, h1);
    }
    else if (diff > 180)
    {
        return GetHueDistance(h2, h1 + 360f);
    }
    return diff;
}

public struct HSVColor
{
    public float h, s, v;
    public HSVColor(float h, float s, float v)
    {
        this.h = h;
        this.s = s;
        this.v = v;
    }

    public static HSVColor FromRGB(Rgb24 color)
    {
        float[] rgb = { color.R / 255f, color.G / 255f, color.B / 255f };
        float max = rgb.Max();
        float min = rgb.Min();
        float hue;
        float saturation = (max - min) / max;
        float value = max;
        if (max == min)
        {
            hue = 0;
            saturation = 0;
        }
        else if (max == rgb[0])
        {
            hue = 60f * (0f + (rgb[1] - rgb[2]) / (max - min));
        }
        else if (max == rgb[1])
        {
            hue = 60f * (2f + (rgb[2] - rgb[0]) / (max - min));
        }
        else
        {
            hue = 60f * (4f + (rgb[0] - rgb[1]) / (max - min));
        }
        if (hue < 0) hue += 360;

        return new(hue, saturation, value);
    }
}