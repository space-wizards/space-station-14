using Content.Shared.Popups;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client.Popups;

/// <summary>
/// Draws popup text, either in world or on screen.
/// </summary>
public sealed class PopupOverlay : Overlay
{
    private readonly IEntityManager _entManager;
    private readonly PopupSystem _popup;

    private readonly ShaderInstance _shader;
    private readonly Font _smallFont;
    private readonly Font _mediumFont;
    private readonly Font _largeFont;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public PopupOverlay(IEntityManager entManager, IPrototypeManager protoManager, IResourceCache cache, PopupSystem popup)
    {
        _entManager = entManager;
        _popup = popup;

        _shader = protoManager.Index<ShaderPrototype>("unshaded").Instance();
        _smallFont = new VectorFont(cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Italic.ttf"), 10);
        _mediumFont = new VectorFont(cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Italic.ttf"), 12);
        _largeFont = new VectorFont(cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-BoldItalic.ttf"), 14);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.ViewportControl == null)
            return;

        args.DrawingHandle.SetTransform(Matrix3.Identity);
        args.DrawingHandle.UseShader(_shader);

        DrawWorld(args.ScreenHandle, args);
        DrawScreen(args.ScreenHandle, args);

        args.DrawingHandle.UseShader(null);
    }

    private void DrawWorld(DrawingHandleScreen worldHandle, OverlayDrawArgs args)
    {
        if (_popup.WorldLabels.Count == 0)
            return;

        var matrix = args.ViewportControl!.GetWorldToScreenMatrix();

        foreach (var popup in _popup.WorldLabels)
        {
            var mapPos = popup.InitialPos.ToMap(_entManager);

            if (mapPos.MapId != args.MapId)
                continue;

            if (!args.WorldAABB.Contains(mapPos.Position))
                continue;

            var pos = matrix.Transform(mapPos.Position);
            DrawPopup(popup, worldHandle, pos);
        }
    }

    private void DrawScreen(DrawingHandleScreen screenHandle, OverlayDrawArgs args)
    {
        foreach (var popup in _popup.CursorLabels)
        {
            // Different window
            if (popup.InitialPos.Window != args.ViewportControl?.Window?.Id)
                continue;

            DrawPopup(popup, screenHandle, popup.InitialPos.Position);
        }
    }

    private void DrawPopup(PopupSystem.PopupLabel popup, DrawingHandleScreen handle, Vector2 position)
    {
        const float alphaMinimum = 0.5f;

        var alpha = MathF.Min(1f, 1f - (popup.TotalTime - alphaMinimum) / (PopupSystem.PopupLifetime - alphaMinimum));
        var updatedPosition = position - new Vector2(0f, 20f * (popup.TotalTime * popup.TotalTime + popup.TotalTime));
        var font = _smallFont;
        var dimensions = Vector2.Zero;
        var color = Color.White.WithAlpha(alpha);

        switch (popup.Type)
        {
            case PopupType.SmallCaution:
                color = Color.Red;
                break;
            case PopupType.Medium:
                font = _mediumFont;
                color = Color.LightGray;
                break;
            case PopupType.MediumCaution:
                font = _mediumFont;
                color = Color.Red;
                break;
            case PopupType.Large:
                font = _largeFont;
                color = Color.LightGray;
                break;
            case PopupType.LargeCaution:
                font = _largeFont;
                color = Color.Red;
                break;
        }

        dimensions = handle.GetDimensions(font, popup.Text, 1f);
        handle.DrawString(font, updatedPosition - dimensions / 2f, popup.Text, color.WithAlpha(alpha));
    }
}
