using Content.Client.Resources;
using Content.Shared.Overlays;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Utility;

namespace Content.Client.Overlays;
/// <summary>
/// Creates overlay image placed over user screen
/// </summary>
public sealed class ImageOverlay : Overlay
{
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    private readonly List<(ResPath Path, Color Color)> _texturesToDraw = new();

    public ImageOverlay()
    {
        IoCManager.InjectDependencies(this);
    }

    public void UpdateState(List<ImageOverlayComponent> components)
    {
        _texturesToDraw.Clear();
        foreach (var comp in components)
        {
            _texturesToDraw.Add((comp.PathToOverlayImage, comp.AdditionalColorOverlay));
        }
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        foreach (var (path, color) in _texturesToDraw)
        {
            var texture = _resourceCache.GetTexture(path);
            var sc = args.ScreenHandle;

            sc.DrawRect(args.ViewportBounds, color);
            sc.DrawTextureRect(texture, args.ViewportBounds);
        }
    }
}
