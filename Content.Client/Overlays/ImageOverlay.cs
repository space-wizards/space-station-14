using System.Numerics;
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
public sealed partial class ImageOverlay : Overlay
{
    [Dependency] private IResourceCache _resourceCache = default!;
    [Dependency] private IEyeManager _eyeManager = default!;

    /// <inheritdoc/>
    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    private readonly List<(ResPath Path, Color Color, Vector2 Scale)> _texturesToDraw = new();

    public ImageOverlay()
    {
        IoCManager.InjectDependencies(this);
    }

    /// <summary>
    /// updates the list of active overlay textures.
    /// </summary>
    public void UpdateState(List<ImageOverlayComponent> components)
    {
        _texturesToDraw.Clear();
        foreach (var comp in components)
        {
            if (comp.Active)
                _texturesToDraw.Add((comp.PathToOverlayImage, comp.AdditionalColorOverlay, comp.Scale));
        }
    }

    /// <summary>
    /// Activates or deactivates the overlay texture.
    /// </summary>
    public void SetActive(ImageOverlayComponent comp, bool isActive)
    {
        if (comp.Active == isActive) return; // prevents repetitious calls of this method
        comp.Active = isActive;

        var overlayPair = (comp.PathToOverlayImage, comp.AdditionalColorOverlay, comp.Scale);
        if (isActive)
            _texturesToDraw.Add(overlayPair);
        else
            _texturesToDraw.Remove(overlayPair);
    }

    /// <inheritdoc />
    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.Viewport.Eye != _eyeManager.CurrentEye)
            return;

        var zoomFactor = _eyeManager.CurrentEye.Zoom.X;
        var screenRect = args.ViewportBounds;

        foreach (var (path, color, scale) in _texturesToDraw)
        {
            var texture = _resourceCache.GetTexture(path);
            var sc = args.ScreenHandle;

            sc.DrawRect(screenRect, color);

            var regionWidth = texture.Width * (1 / scale.X) * zoomFactor;
            var regionHeight = texture.Height * (1 / scale.Y) * zoomFactor;

            var left = (texture.Width / 2f) - (regionWidth / 2f);
            var top = (texture.Height / 2f) - (regionHeight / 2f);

            var subRegion = UIBox2.FromDimensions(left, top, regionWidth, regionHeight);

            sc.DrawTextureRectRegion(texture, screenRect, subRegion);
        }
    }
}
