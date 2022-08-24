using System.Reflection.Metadata;
using Content.Shared.Popups;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client.Popups;

/// <summary>
/// Draws world popups. Screen / cursor popups are implemented as labels.
/// </summary>
public sealed class PopupOverlay : Overlay
{
    private IEntityManager _entManager;
    private PopupSystem _popup;

    private readonly ShaderInstance _shader;
    private readonly Font _smallFont;
    private readonly Font _mediumFont;
    private readonly Font _largeFont;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV | OverlaySpace.ScreenSpace;

    public PopupOverlay(IEntityManager entManager, IResourceCache cache, PopupSystem popup)
    {
        _entManager = entManager;
        _popup = popup;

        _shader = IoCManager.Resolve<IPrototypeManager>().Index<ShaderPrototype>("unshaded").Instance();
        _smallFont = new VectorFont(cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Italic.ttf"), 10);
        _mediumFont = new VectorFont(cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Italic.ttf"), 12);
        _largeFont = new VectorFont(cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-BoldItalic.ttf"), 14);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        args.DrawingHandle.UseShader(_shader);

        switch (args.DrawingHandle)
        {
            case DrawingHandleWorld worldHandle:
                DrawWorld(worldHandle, args);
                break;
            case DrawingHandleScreen screenHandle:
                DrawScreen(screenHandle, args);
                break;
        }

        args.DrawingHandle.UseShader(null);
    }

    private void DrawWorld(DrawingHandleWorld worldHandle, OverlayDrawArgs args)
    {
        if (_popup.WorldLabels.Count == 0)
            return;

        var rotation = args.Viewport.Eye?.Rotation ?? Angle.Zero;
        var rotationMatrix = Matrix3.CreateRotation(-rotation);

        foreach (var popup in _popup.WorldLabels)
        {
            var mapPos = popup.InitialPos.ToMap(_entManager);

            if (mapPos.MapId != args.MapId)
                continue;

            if (!args.WorldAABB.Contains(mapPos.Position))
                continue;

            var worldMatrix = Matrix3.CreateTranslation(mapPos.Position);
            Matrix3.Multiply(rotationMatrix, worldMatrix, out var matty);
            worldHandle.SetTransform(matty);

            var position = Vector2.Zero;

            DrawPopup(popup, worldHandle, position);
        }

        worldHandle.SetTransform(Matrix3.Identity);
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

    private void DrawPopup(PopupSystem.PopupLabel popup, DrawingHandleBase handle, Vector2 position)
    {
        const float alphaMinimum = 0.5f;

        var alpha = MathF.Max(1f, 1f - (popup.TotalTime - alphaMinimum) / (PopupSystem.PopupLifetime - alphaMinimum));

        switch (popup.Type)
        {
            case PopupType.SmallCaution:
                handle.DrawString(_smallFont, position, popup.Text, Color.Red.WithAlpha(alpha));
                break;
            case PopupType.Medium:
                handle.DrawString(_mediumFont, position, popup.Text, Color.LightGray.WithAlpha(alpha));
                break;
            case PopupType.MediumCaution:
                handle.DrawString(_mediumFont, position, popup.Text, Color.Red.WithAlpha(alpha));
                break;
            case PopupType.Large:
                handle.DrawString(_largeFont, position, popup.Text, Color.LightGray.WithAlpha(alpha));
                break;
            case PopupType.LargeCaution:
                handle.DrawString(_largeFont, position, popup.Text, Color.Red.WithAlpha(alpha));
                break;
            case PopupType.Small:
            default:
                handle.DrawString(_smallFont, position, popup.Text, Color.White.WithAlpha(alpha));
                break;
        }
    }
}
