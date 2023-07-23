﻿using System.Drawing;
using System.Text.Json.Serialization;

namespace BlazorWebAssembly.Client.Shared
{

    [Serializable]
    public class TextureMetadata
    {
        [JsonInclude]
        public string fileName;
        [JsonInclude]
        public string blockName;
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
    }

    [Serializable]
    public struct RGB
    {
        [JsonInclude]
        public byte r, g, b;
        public static implicit operator RGB(System.Drawing.Color c) => new() { r = c.R, g = c.G, b = c.B };

        public override string ToString()
        {
            return $"RGB({r},{g},{b})";
        }

        public string AsHexString()
        {
            return $"#{Convert.ToHexString(new byte[] { r, g, b })}";
        }

        public int RGBDistance(RGB other)
        {
            return Math.Abs((int)r - (int)other.r) + Math.Abs((int)g - (int)other.g) + Math.Abs((int)b - (int)other.b);
        }
    }
}