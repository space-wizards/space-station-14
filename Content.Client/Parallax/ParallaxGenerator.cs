using System;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Nett;
using Robust.Client.Utility;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Noise;
using Robust.Shared.Random;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = Robust.Shared.Maths.Color;

namespace Content.Client.Parallax
{
    public sealed class ParallaxGenerator
    {
        private readonly List<Layer> Layers = new();

        public static Image<Rgba32> GenerateParallax(TomlTable config, Size size, ISawmill sawmill, List<Image<Rgba32>>? debugLayerDump, CancellationToken cancel = default)
        {
            sawmill.Debug("Generating parallax!");
            var generator = new ParallaxGenerator();
            generator._loadConfig(config);

            sawmill.Debug("Timing start!");
            var sw = new Stopwatch();
            sw.Start();
            var image = new Image<Rgba32>(Configuration.Default, size.Width, size.Height, new Rgba32(0, 0, 0, 0));
            var count = 0;
            foreach (var layer in generator.Layers)
            {
                cancel.ThrowIfCancellationRequested();
                layer.Apply(image);
                debugLayerDump?.Add(image.Clone());
                sawmill.Debug("Layer {0} done!", count++);
            }

            sw.Stop();
            sawmill.Debug("Total time: {0}", sw.Elapsed.TotalSeconds);

            return image;
        }

        private void _loadConfig(TomlTable config)
        {
            foreach (var layerArray in ((TomlTableArray) config.Get("layers")).Items)
            {
                switch (((TomlValue<string>) layerArray.Get("type")).Value)
                {
                    case "clear":
                        var layerClear = new LayerClear(layerArray);
                        Layers.Add(layerClear);
                        break;

                    case "toalpha":
                        var layerToAlpha = new LayerToAlpha(layerArray);
                        Layers.Add(layerToAlpha);
                        break;

                    case "noise":
                        var layerNoise = new LayerNoise(layerArray);
                        Layers.Add(layerNoise);
                        break;

                    case "points":
                        var layerPoint = new LayerPoints(layerArray);
                        Layers.Add(layerPoint);
                        break;

                    default:
                        throw new NotSupportedException();
                }
            }
        }

        private abstract class Layer
        {
            public abstract void Apply(Image<Rgba32> bitmap);
        }

        private abstract class LayerConversion : Layer
        {
            public abstract Color ConvertColor(Color input);

            public override void Apply(Image<Rgba32> bitmap)
            {
                var span = bitmap.GetPixelSpan();

                for (var y = 0; y < bitmap.Height; y++)
                {
                    for (var x = 0; x < bitmap.Width; x++)
                    {
                        var i = y * bitmap.Width + x;
                        span[i] = ConvertColor(span[i].ConvertImgSharp()).ConvertImgSharp();
                    }
                }
            }
        }

        private sealed class LayerClear : LayerConversion
        {
            private readonly Color Color = Color.Black;

            public LayerClear(TomlTable table)
            {
                if (table.TryGetValue("color", out var tomlObject))
                {
                    Color = Color.FromHex(((TomlValue<string>) tomlObject).Value);
                }
            }

            public override Color ConvertColor(Color input) => Color;
        }

        private sealed class LayerToAlpha : LayerConversion
        {
            public LayerToAlpha(TomlTable table)
            {
            }

            public override Color ConvertColor(Color input)
            {
                return new Color(input.R, input.G, input.B, MathF.Min(input.R + input.G + input.B, 1.0f));
            }
        }

        private sealed class LayerNoise : Layer
        {
            private readonly Color InnerColor = Color.White;
            private readonly Color OuterColor = Color.Black;
            private readonly NoiseGenerator.NoiseType NoiseType = NoiseGenerator.NoiseType.Fbm;
            private readonly uint Seed = 1234;
            private readonly float Persistence = 0.5f;
            private readonly float Lacunarity = (float) (Math.PI / 3);
            private readonly float Frequency = 1;
            private readonly uint Octaves = 3;
            private readonly float Threshold;
            private readonly float Power = 1;
            private readonly Color.BlendFactor SrcFactor = Color.BlendFactor.One;
            private readonly Color.BlendFactor DstFactor = Color.BlendFactor.One;

            public LayerNoise(TomlTable table)
            {
                if (table.TryGetValue("innercolor", out var tomlObject))
                {
                    InnerColor = Color.FromHex(((TomlValue<string>) tomlObject).Value);
                }

                if (table.TryGetValue("outercolor", out tomlObject))
                {
                    OuterColor = Color.FromHex(((TomlValue<string>) tomlObject).Value);
                }

                if (table.TryGetValue("seed", out tomlObject))
                {
                    Seed = (uint) ((TomlValue<long>) tomlObject).Value;
                }

                if (table.TryGetValue("persistence", out tomlObject))
                {
                    Persistence = float.Parse(((TomlValue<string>) tomlObject).Value, CultureInfo.InvariantCulture);
                }

                if (table.TryGetValue("lacunarity", out tomlObject))
                {
                    Lacunarity = float.Parse(((TomlValue<string>) tomlObject).Value, CultureInfo.InvariantCulture);
                }

                if (table.TryGetValue("frequency", out tomlObject))
                {
                    Frequency = float.Parse(((TomlValue<string>) tomlObject).Value, CultureInfo.InvariantCulture);
                }

                if (table.TryGetValue("octaves", out tomlObject))
                {
                    Octaves = (uint) ((TomlValue<long>) tomlObject).Value;
                }

                if (table.TryGetValue("threshold", out tomlObject))
                {
                    Threshold = float.Parse(((TomlValue<string>) tomlObject).Value, CultureInfo.InvariantCulture);
                }

                if (table.TryGetValue("sourcefactor", out tomlObject))
                {
                    SrcFactor = (Color.BlendFactor) Enum.Parse(typeof(Color.BlendFactor), ((TomlValue<string>) tomlObject).Value);
                }

                if (table.TryGetValue("destfactor", out tomlObject))
                {
                    DstFactor = (Color.BlendFactor) Enum.Parse(typeof(Color.BlendFactor), ((TomlValue<string>) tomlObject).Value);
                }

                if (table.TryGetValue("power", out tomlObject))
                {
                    Power = float.Parse(((TomlValue<string>) tomlObject).Value, CultureInfo.InvariantCulture);
                }

                if (table.TryGetValue("noise_type", out tomlObject))
                {
                    switch (((TomlValue<string>) tomlObject).Value)
                    {
                        case "fbm":
                            NoiseType = NoiseGenerator.NoiseType.Fbm;
                            break;
                        case "ridged":
                            NoiseType = NoiseGenerator.NoiseType.Ridged;
                            break;
                        default:
                            throw new InvalidOperationException();
                    }
                }
            }

            public override void Apply(Image<Rgba32> bitmap)
            {
                var noise = new NoiseGenerator(NoiseType);
                noise.SetSeed(Seed);
                noise.SetFrequency(Frequency);
                noise.SetPersistence(Persistence);
                noise.SetLacunarity(Lacunarity);
                noise.SetOctaves(Octaves);
                noise.SetPeriodX(bitmap.Width);
                noise.SetPeriodY(bitmap.Height);
                var threshVal = 1 / (1 - Threshold);
                var powFactor = 1 / Power;

                var span = bitmap.GetPixelSpan();

                for (var y = 0; y < bitmap.Height; y++)
                {
                    for (var x = 0; x < bitmap.Width; x++)
                    {
                        // Do noise calculations.
                        var noiseVal = MathF.Min(1, MathF.Max(0, (noise.GetNoise(x, y) + 1) / 2));

                        // Threshold
                        noiseVal = MathF.Max(0, noiseVal - Threshold);
                        noiseVal *= threshVal;
                        noiseVal = MathF.Pow(noiseVal, powFactor);

                        // Get colors based on noise values.
                        var srcColor = Color.InterpolateBetween(OuterColor, InnerColor, noiseVal)
                            .WithAlpha(noiseVal);

                        // Apply blending factors & write back.
                        var i = y * bitmap.Width + x;
                        var dstColor = span[i].ConvertImgSharp();
                        span[i] = Color.Blend(dstColor, srcColor, DstFactor, SrcFactor).ConvertImgSharp();
                    }
                }
            }
        }

        private sealed class LayerPoints : Layer
        {
            private readonly int Seed = 1234;
            private readonly int PointCount = 100;

            private readonly Color CloseColor = Color.White;
            private readonly Color FarColor = Color.Black;

            private readonly Color.BlendFactor SrcFactor = Color.BlendFactor.One;
            private readonly Color.BlendFactor DstFactor = Color.BlendFactor.One;

            // Noise mask stuff.
            private readonly bool Masked;
            private readonly NoiseGenerator.NoiseType MaskNoiseType = NoiseGenerator.NoiseType.Fbm;
            private readonly uint MaskSeed = 1234;
            private readonly float MaskPersistence = 0.5f;
            private readonly float MaskLacunarity = (float) (Math.PI * 2 / 3);
            private readonly float MaskFrequency = 1;
            private readonly uint MaskOctaves = 3;
            private readonly float MaskThreshold;
            private readonly int PointSize = 1;
            private readonly float MaskPower = 1;


            public LayerPoints(TomlTable table)
            {
                if (table.TryGetValue("seed", out var tomlObject))
                {
                    Seed = (int) ((TomlValue<long>) tomlObject).Value;
                }

                if (table.TryGetValue("count", out tomlObject))
                {
                    PointCount = (int) ((TomlValue<long>) tomlObject).Value;
                }

                if (table.TryGetValue("sourcefactor", out tomlObject))
                {
                    SrcFactor = (Color.BlendFactor) Enum.Parse(typeof(Color.BlendFactor), ((TomlValue<string>) tomlObject).Value);
                }

                if (table.TryGetValue("destfactor", out tomlObject))
                {
                    DstFactor = (Color.BlendFactor) Enum.Parse(typeof(Color.BlendFactor), ((TomlValue<string>) tomlObject).Value);
                }

                if (table.TryGetValue("farcolor", out tomlObject))
                {
                    FarColor = Color.FromHex(((TomlValue<string>) tomlObject).Value);
                }

                if (table.TryGetValue("closecolor", out tomlObject))
                {
                    CloseColor = Color.FromHex(((TomlValue<string>) tomlObject).Value);
                }

                if (table.TryGetValue("pointsize", out tomlObject))
                {
                    PointSize = (int) ((TomlValue<long>) tomlObject).Value;
                }

                // Noise mask stuff.
                if (table.TryGetValue("mask", out tomlObject))
                {
                    Masked = ((TomlValue<bool>) tomlObject).Value;
                }

                if (table.TryGetValue("maskseed", out tomlObject))
                {
                    MaskSeed = (uint) ((TomlValue<long>) tomlObject).Value;
                }

                if (table.TryGetValue("maskpersistence", out tomlObject))
                {
                    MaskPersistence = float.Parse(((TomlValue<string>) tomlObject).Value, CultureInfo.InvariantCulture);
                }

                if (table.TryGetValue("masklacunarity", out tomlObject))
                {
                    MaskLacunarity = float.Parse(((TomlValue<string>) tomlObject).Value, CultureInfo.InvariantCulture);
                }

                if (table.TryGetValue("maskfrequency", out tomlObject))
                {
                    MaskFrequency = float.Parse(((TomlValue<string>) tomlObject).Value, CultureInfo.InvariantCulture);
                }

                if (table.TryGetValue("maskoctaves", out tomlObject))
                {
                    MaskOctaves = (uint) ((TomlValue<long>) tomlObject).Value;
                }

                if (table.TryGetValue("maskthreshold", out tomlObject))
                {
                    MaskThreshold = float.Parse(((TomlValue<string>) tomlObject).Value, CultureInfo.InvariantCulture);
                }

                if (table.TryGetValue("masknoise_type", out tomlObject))
                {
                    switch (((TomlValue<string>) tomlObject).Value)
                    {
                        case "fbm":
                            MaskNoiseType = NoiseGenerator.NoiseType.Fbm;
                            break;
                        case "ridged":
                            MaskNoiseType = NoiseGenerator.NoiseType.Ridged;
                            break;
                        default:
                            throw new InvalidOperationException();
                    }
                }

                if (table.TryGetValue("maskpower", out tomlObject))
                {
                    MaskPower = float.Parse(((TomlValue<string>) tomlObject).Value, CultureInfo.InvariantCulture);
                }
            }

            public override void Apply(Image<Rgba32> bitmap)
            {
                // Temporary buffer so we don't mess up blending.
                using (var buffer = new Image<Rgba32>(Configuration.Default, bitmap.Width, bitmap.Height, new Rgba32(0, 0, 0, 0)))
                {
                    if (Masked)
                    {
                        GenPointsMasked(buffer);
                    }
                    else
                    {
                        GenPoints(buffer);
                    }

                    var srcSpan = buffer.GetPixelSpan();
                    var dstSpan = bitmap.GetPixelSpan();

                    var width = bitmap.Width;
                    var height = bitmap.Height;

                    for (var y = 0; y < height; y++)
                    {
                        for (var x = 0; x < width; x++)
                        {
                            var i = y * width + x;

                            var dstColor = dstSpan[i].ConvertImgSharp();
                            var srcColor = srcSpan[i].ConvertImgSharp();

                            dstSpan[i] = Color.Blend(dstColor, srcColor, DstFactor, SrcFactor).ConvertImgSharp();
                        }
                    }
                }
            }

            private void GenPoints(Image<Rgba32> buffer)
            {
                var o = PointSize - 1;
                var random = new Random(Seed);
                var span = buffer.GetPixelSpan();

                for (var i = 0; i < PointCount; i++)
                {
                    var x = random.Next(0, buffer.Width);
                    var y = random.Next(0, buffer.Height);

                    var dist = random.NextFloat();

                    for (var oy = y - o; oy <= y + o; oy++)
                    {
                        for (var ox = x - o; ox <= x + o; ox++)
                        {
                            var ix = MathHelper.Mod(ox, buffer.Width);
                            var iy = MathHelper.Mod(oy, buffer.Height);

                            var color = Color.InterpolateBetween(FarColor, CloseColor, dist).ConvertImgSharp();
                            span[iy * buffer.Width + ix] = color;
                        }
                    }
                }
            }

            private void GenPointsMasked(Image<Rgba32> buffer)
            {
                var o = PointSize - 1;
                var random = new Random(Seed);
                var noise = new NoiseGenerator(MaskNoiseType);
                noise.SetSeed(MaskSeed);
                noise.SetFrequency(MaskFrequency);
                noise.SetPersistence(MaskPersistence);
                noise.SetLacunarity(MaskLacunarity);
                noise.SetOctaves(MaskOctaves);
                noise.SetPeriodX(buffer.Width);
                noise.SetPeriodY(buffer.Height);

                var threshVal = 1 / (1 - MaskThreshold);
                var powFactor = 1 / MaskPower;

                const int maxPointAttemptCount = 9999;
                var pointAttemptCount = 0;

                var span = buffer.GetPixelSpan();

                for (var i = 0; i < PointCount; i++)
                {
                    var x = random.Next(0, buffer.Width);
                    var y = random.Next(0, buffer.Height);

                    // Grab noise at this point.
                    var noiseVal = MathF.Min(1, MathF.Max(0, (noise.GetNoise(x, y) + 1) / 2));
                    // Threshold
                    noiseVal = MathF.Max(0, noiseVal - MaskThreshold);
                    noiseVal *= threshVal;
                    noiseVal = MathF.Pow(noiseVal, powFactor);

                    var randomThresh = random.NextFloat();
                    if (randomThresh > noiseVal)
                    {
                        if (++pointAttemptCount <= maxPointAttemptCount)
                        {
                            i--;
                        }

                        continue;
                    }

                    var dist = random.NextFloat();

                    for (var oy = y - o; oy <= y + o; oy++)
                    {
                        for (var ox = x - o; ox <= x + o; ox++)
                        {
                            var ix = MathHelper.Mod(ox, buffer.Width);
                            var iy = MathHelper.Mod(oy, buffer.Height);

                            var color = Color.InterpolateBetween(FarColor, CloseColor, dist).ConvertImgSharp();
                            span[iy * buffer.Width + ix] = color;
                        }
                    }
                }
            }
        }
    }
}
