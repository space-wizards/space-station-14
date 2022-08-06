using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.MapRenderer;

public sealed class RenderedGridImage <T> where T : unmanaged, IPixel<T>
{
    public Image<T> Image;
    public Vector2 Offset { get; set; } = Vector2.Zero;
    public EntityUid? GridUid { get; set; }

    public RenderedGridImage(Image<T> image)
    {
        Image = image;
    }
}
