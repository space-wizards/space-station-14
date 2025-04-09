using System.Text;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.Utility;
using Robust.Shared.Graphics;
using Robust.Shared.Graphics.RSI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client.Clickable
{
    internal sealed class ClickMapManager : IClickMapManager, IPostInjectInit
    {
        private static readonly string[] IgnoreTexturePaths =
        {
            // These will probably never need click maps so skip em.
            "/Textures/Interface",
            "/Textures/LobbyScreens",
            "/Textures/Parallaxes",
            "/Textures/Logo",
        };

        private const float Threshold = 0.1f;
        private const int ClickRadius = 2;

        [Dependency] private readonly IResourceCache _resourceCache = default!;

        [ViewVariables]
        private readonly Dictionary<Texture, ClickMap> _textureMaps = new();

        [ViewVariables] private readonly Dictionary<RSI, RsiClickMapData> _rsiMaps =
            new();

        public void PostInject()
        {
            _resourceCache.OnRawTextureLoaded += OnRawTextureLoaded;
            _resourceCache.OnRsiLoaded += OnOnRsiLoaded;
        }

        private void OnOnRsiLoaded(RsiLoadedEventArgs obj)
        {
            if (obj.Atlas is Image<Rgba32> rgba)
            {
                var clickMap = ClickMap.FromImage(rgba, Threshold);

                var rsiData = new RsiClickMapData(clickMap, obj.AtlasOffsets);
                _rsiMaps[obj.Resource.RSI] = rsiData;
            }
        }

        private void OnRawTextureLoaded(TextureLoadedEventArgs obj)
        {
            if (obj.Image is Image<Rgba32> rgba)
            {
                var pathStr = obj.Path.ToString();
                foreach (var path in IgnoreTexturePaths)
                {
                    if (pathStr.StartsWith(path, StringComparison.Ordinal))
                        return;
                }

                _textureMaps[obj.Resource] = ClickMap.FromImage(rgba, Threshold);
            }
        }

        public bool IsOccluding(Texture texture, Vector2i pos)
        {
            if (!_textureMaps.TryGetValue(texture, out var clickMap))
            {
                return false;
            }

            return SampleClickMap(clickMap, pos, clickMap.Size, Vector2i.Zero);
        }

        public bool IsOccluding(RSI rsi, RSI.StateId state, RsiDirection dir, int frame, Vector2i pos)
        {
            if (!_rsiMaps.TryGetValue(rsi, out var rsiData))
            {
                return false;
            }

            if (!rsiData.Offsets.TryGetValue(state, out var stateDat) || stateDat.Length <= (int) dir)
            {
                return false;
            }

            var dirDat = stateDat[(int) dir];
            if (dirDat.Length <= frame)
            {
                return false;
            }

            var offset = dirDat[frame];
            return SampleClickMap(rsiData.ClickMap, pos, rsi.Size, offset);
        }

        private static bool SampleClickMap(ClickMap map, Vector2i pos, Vector2i bounds, Vector2i offset)
        {
            var (width, height) = bounds;
            var (px, py) = pos;

            for (var x = -ClickRadius; x <= ClickRadius; x++)
            {
                var ox = px + x;
                if (ox < 0 || ox >= width)
                {
                    continue;
                }

                for (var y = -ClickRadius; y <= ClickRadius; y++)
                {
                    var oy = py + y;

                    if (oy < 0 || oy >= height)
                    {
                        continue;
                    }

                    if (map.IsOccluded((ox, oy) + offset))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private sealed class RsiClickMapData
        {
            public readonly ClickMap ClickMap;
            public readonly Dictionary<RSI.StateId, Vector2i[][]> Offsets;

            public RsiClickMapData(ClickMap clickMap, Dictionary<RSI.StateId, Vector2i[][]> offsets)
            {
                ClickMap = clickMap;
                Offsets = offsets;
            }
        }

        internal sealed class ClickMap
        {
            [ViewVariables] private readonly byte[] _data;

            public int Width { get; }
            public int Height { get; }
            [ViewVariables] public Vector2i Size => (Width, Height);

            public bool IsOccluded(int x, int y)
            {
                var i = y * Width + x;
                return (_data[i / 8] & (1 << (i % 8))) != 0;
            }

            public bool IsOccluded(Vector2i vector)
            {
                var (x, y) = vector;
                return IsOccluded(x, y);
            }

            private ClickMap(byte[] data, int width, int height)
            {
                Width = width;
                Height = height;
                _data = data;
            }

            public static ClickMap FromImage<T>(Image<T> image, float threshold) where T : unmanaged, IPixel<T>
            {
                var threshByte = (byte) (threshold * 255);
                var width = image.Width;
                var height = image.Height;

                var dataSize = (int) Math.Ceiling(width * height / 8f);
                var data = new byte[dataSize];

                var pixelSpan = image.GetPixelSpan();

                for (var i = 0; i < pixelSpan.Length; i++)
                {
                    Rgba32 rgba = default;
                    pixelSpan[i].ToRgba32(ref rgba);
                    if (rgba.A >= threshByte)
                    {
                        data[i / 8] |= (byte) (1 << (i % 8));
                    }
                }

                return new ClickMap(data, width, height);
            }

            public string DumpText()
            {
                var sb = new StringBuilder();
                for (var y = 0; y < Height; y++)
                {
                    for (var x = 0; x < Width; x++)
                    {
                        sb.Append(IsOccluded(x, y) ? "1" : "0");
                    }

                    sb.AppendLine();
                }

                return sb.ToString();
            }
        }
    }

    public interface IClickMapManager
    {
        public bool IsOccluding(Texture texture, Vector2i pos);

        public bool IsOccluding(RSI rsi, RSI.StateId state, RsiDirection dir, int frame, Vector2i pos);
    }
}
