using Img2mc.Shared;
using System.Collections.Concurrent;

namespace BlazorWebAssembly.Client.Core;

public class ImageReconstructor
{
    public MinecraftBlock[] blockData = Array.Empty<MinecraftBlock>(); 

    public TextureMetadata[,] OutputTextures { get; private set; } = new TextureMetadata[0,0];
    public int OutputRows { get; private set; } = 0;
    public int OutputColumns { get; private set; } = 0;
    public int MaxOutputImageSize { get; set; } = 128;

    public event Action? OnOutputChanged;

    public readonly Dictionary<TextureMetadata, int> textureCounts = new();

    public async void ReadFile(Stream stream)
    {
        try
        {
            using Image<Rgba32> img = await Image.LoadAsync<Rgba32>(stream);
            Console.WriteLine($"Read the file as image: {img}");
            var px = img[0, 0];

            if (img.Width > MaxOutputImageSize)
            {
                img.Mutate(x => x.Resize(MaxOutputImageSize, 0));
            }
            else if (img.Height > MaxOutputImageSize)
            {
                img.Mutate(x => x.Resize(0, MaxOutputImageSize));
            }
            Console.WriteLine($"Resized image to {img}");
            OutputColumns = img.Width;
            OutputRows = img.Height;
            var buffer = new TextureMetadata[OutputColumns, OutputRows];
            for (int x = 0; x < OutputColumns; x++)
            {
                for (int y = 0; y < OutputRows; y++)
                {
                    buffer[x, y] = FindBestMatchingTexture(img[x, y]);
                }
            }
            Console.WriteLine("Done processing image.");
            textureCounts.Clear();
            foreach (var tex in buffer)
            {
                if (textureCounts.ContainsKey(tex))
                {
                    textureCounts[tex]++;
                }
                else
                {
                    textureCounts[tex] = 1;
                }
            }
            OutputTextures = buffer;
            img.Dispose();
            OnOutputChanged?.Invoke();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private TextureMetadata FindBestMatchingTexture(Rgba32 pixel)
    {
        RGB col = new() { r = pixel.R, g = pixel.G, b = pixel.B };
        return blockData.OrderBy(x => x.textures[0].averageRGB.RGBDistance(col)).Select(x => x.textures[0]).First();
    }

}
