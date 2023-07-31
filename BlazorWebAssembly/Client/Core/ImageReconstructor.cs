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
    public float GreedyTextureThreshold { get; set; } = -1;

    public event Action? OnOutputChanged;

    public readonly Dictionary<MinecraftBlock, int> blockCounts = new();

    private readonly Dictionary<RGB, (MinecraftBlock, TextureMetadata)> knownRGBs = new();

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

            knownRGBs.Clear();
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

        if(knownRGBs.ContainsKey(col))
        {
            Console.WriteLine($"KNOWN!: {col}");
			return knownRGBs[col];
		}

        float minDist = float.MaxValue;
        MinecraftBlock? selectedBlock = null;
        TextureMetadata? selectedTexture = null;

		for (int block_i = 0; block_i < blockData.Length; block_i++)
        {
            var block = blockData[block_i];
            if (block.exclude)
                continue;
            for(int tex_i = 0; tex_i < block.textures.Length; tex_i++)
            {
                var tex = block.textures[tex_i];
                if (tex.exclude) 
                    continue;
                float dist = tex.ColorDistance(col, ContrastBias);
                if(dist <= GreedyTextureThreshold || dist == 0)
				{
					knownRGBs.Add(col, (block, tex));
                    Console.WriteLine($"GREEDY!: {col}");
					return (block, tex);
                }
                else if(dist < minDist)
                {
                    selectedBlock = block; 
                    selectedTexture = tex;
                    minDist = dist;
                }
            }
        }

        if (selectedBlock == null || selectedTexture == null)
            throw new Exception("No texture could be found.");

        knownRGBs.Add(col, (selectedBlock, selectedTexture));
        return (selectedBlock, selectedTexture);
    }

	/**
	 * old code for FindBestMachingTexture. It wont be lost in git anyway, but i want to keep it in here as a reminder.
     * var query = from block in blockData
                    from texture in block.textures
                    orderby texture.ColorDistance(block, col, ContrastBias) ascending
                    select (block, texture);
        return query.First();
     */


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
