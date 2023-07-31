using Img2mc.Shared;
using Microsoft.AspNetCore.Components;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using System.Collections.Concurrent;

namespace BlazorWebAssembly.Client.Core;

public class ImageReconstructor
{
    public MinecraftBlock[] blockData = Array.Empty<MinecraftBlock>();

    [Inject]
    public HttpClient? HttpClient { get; set; }

    public OutputTexture[,] OutputTextures { get; private set; } = new OutputTexture[0,0];
    public int OutputRows { get; private set; } = 0;
    public int OutputColumns { get; private set; } = 0;
    public int MaxOutputImageSize { get; set; } = 128;
    public float ContrastBias { get; set; } = 2f;

    public event Action? OnOutputChanged;

    public readonly Dictionary<MinecraftBlock, int> blockCounts = new();

    public async void ReadFile(Stream stream)
    {
        try
        {
            using Image<Rgba32> img = await Image.LoadAsync<Rgba32>(stream);
            Console.WriteLine($"Read the file as image: {img}");
            blockCounts.Clear();
            var px = img[0, 0];
            // Available Resamplers:
            // Bicubic NearestNeighbor Box MitchellNetravali CatmullRom Lanczos2 Lanczos3 Lanczos5 Lanczos8 Welch Robidoux RobidouxSharp Spline Hermite
            if (img.Width > MaxOutputImageSize)
            {
                img.Mutate(x => x.Resize(MaxOutputImageSize, 0, new BicubicResampler()));
            }
            else if (img.Height > MaxOutputImageSize)
            {
                img.Mutate(x => x.Resize(0, MaxOutputImageSize, new BicubicResampler()));
            }
            Console.WriteLine($"Resized image to {img}");
            OutputColumns = img.Width;
            OutputRows = img.Height;
            var buffer = new OutputTexture[OutputColumns, OutputRows];
            for (int x = 0; x < OutputColumns; x++)
            {
                for (int y = 0; y < OutputRows; y++)
                {
                    var result = FindBestMatchingTexture(img[x, y]);
                    buffer[x, y] = new(result.Item1.blockName, result.Item2.fileName);
                    if (blockCounts.ContainsKey(result.Item1))
                    {
                        blockCounts[result.Item1]++;
                    }
                    else
                    {
                        blockCounts[result.Item1] = 1;
                    }
                }
            }
            Console.WriteLine("Done processing image.");
            OutputTextures = buffer;
            img.Dispose();
            OnOutputChanged?.Invoke();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private (MinecraftBlock, TextureMetadata) FindBestMatchingTexture(Rgba32 pixel)
    {
        RGB col = new() { r = pixel.R, g = pixel.G, b = pixel.B };
        var query = from block in blockData
                    from texture in block.textures
                    orderby texture.ColorDistance(col, ContrastBias) ascending
                    select (block, texture);
        return query.First();
    }

    public readonly struct OutputTexture
    {
        public readonly string blockName;
        public readonly string fileName;

        public OutputTexture(string blockName, string fileName)
        {
            this.blockName = blockName;
            this.fileName = fileName;
        }
    }

}
