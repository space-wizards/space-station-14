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
    [Dependency] private readonly IEyeManager _eyeManager = default!;

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
            if (comp.Active)
                _texturesToDraw.Add((comp.PathToOverlayImage, comp.AdditionalColorOverlay));
        }
    }

    public void OverlayActivate(ImageOverlayComponent comp, bool isActive)
    {
        var overlayPair = (comp.PathToOverlayImage, comp.AdditionalColorOverlay);
        comp.Active = isActive;
        if (isActive)
        {
            if (!_texturesToDraw.Contains(overlayPair))
                _texturesToDraw.Add(overlayPair);
        }
        else
            _texturesToDraw.Remove(overlayPair);
    }

protected override void Draw(in OverlayDrawArgs args)
    {
        var zoomFactor = _eyeManager.CurrentEye.Zoom.X;

        var screenRect = args.ViewportBounds;

        foreach (var (path, color) in _texturesToDraw)
        {
            var texture = _resourceCache.GetTexture(path);
            var sc = args.ScreenHandle;

            sc.DrawRect(screenRect, color);

            var regionWidth = texture.Width * zoomFactor;
            var regionHeight = texture.Height * zoomFactor;

            var left = (texture.Width / 2f) - (regionWidth / 2f);
            var top = (texture.Height / 2f) - (regionHeight / 2f);

            var subRegion = UIBox2.FromDimensions(left, top, regionWidth, regionHeight);

            sc.DrawTextureRectRegion(texture, screenRect, subRegion);
        }
    }
}
