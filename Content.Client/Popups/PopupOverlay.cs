using Content.Shared.Popups;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;

namespace Content.Client.Popups;

/// <summary>
/// Draws world popups. Screen / cursor popups are implemented as labels.
/// </summary>
public sealed class PopupOverlay : Overlay
{
    private IEntityManager _entManager;
    private PopupSystem _popup;

    private readonly Font _smallFont;
    private readonly Font _mediumFont;
    private readonly Font _largeFont;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV | OverlaySpace.ScreenSpace;

    public PopupOverlay(IEntityManager entManager, IResourceCache cache, PopupSystem popup)
    {
        _entManager = entManager;
        _popup = popup;

        _smallFont = new VectorFont(cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Italic.ttf"), 10);
        _mediumFont = new VectorFont(cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Italic.ttf"), 12);
        _largeFont = new VectorFont(cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-BoldItalic.ttf"), 14);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        switch (args.DrawingHandle)
        {
            case DrawingHandleWorld worldHandle:
                DrawWorld(worldHandle, args);
                break;
            case DrawingHandleScreen screenHandle:
                DrawScreen(screenHandle, args);
                break;
        }
    }

    private void DrawWorld(DrawingHandleWorld worldHandle, OverlayDrawArgs args)
    {
        foreach (var popup in _popup.WorldLabels)
        {
            var mapPos = popup.InitialPos.ToMap(_entManager);

            if (mapPos.MapId != args.MapId)
                continue;

            // Don't do WorldAABB check as it's most likely in-range and we don't want to clip if the entity goes off screen I guess?
            // TODO: Need to draw oriented vertically to screen (look at DoAfterOverlay?)
            var position = mapPos.Position;

            // worldHandle.
            switch (popup.Type)
            {
                case PopupType.SmallCaution:
                    worldHandle.DrawString(_smallFont, popup.InitialPos.Position, popup.Text, Color.Red);
                    break;
                case PopupType.Medium:
                    worldHandle.DrawString(_mediumFont, popup.InitialPos.Position, popup.Text, Color.LightGray);
                    break;
                case PopupType.MediumCaution:
                    worldHandle.DrawString(_mediumFont, popup.InitialPos.Position, popup.Text, Color.Red);
                    break;
                case PopupType.Large:
                    worldHandle.DrawString(_largeFont, popup.InitialPos.Position, popup.Text, Color.LightGray);
                    break;
                case PopupType.LargeCaution:
                    worldHandle.DrawString(_largeFont, popup.InitialPos.Position, popup.Text, Color.Red);
                    break;
                case PopupType.Small:
                default:
                    worldHandle.DrawString(_smallFont, popup.InitialPos.Position, popup.Text, Color.White);
                    break;
            }

            /*
            LayoutContainer.SetPosition(this, position - (0, 20 * (TotalTime * TotalTime + TotalTime)));
            */
        }
    }

    private void DrawScreen(DrawingHandleScreen screenHandle, OverlayDrawArgs args)
    {
        foreach (var popup in _popup.CursorLabels)
        {
            // Different window
            if (popup.InitialPos.Window != args.ViewportControl?.Window?.Id)
            {
                continue;
            }

            switch (popup.Type)
            {
                case PopupType.SmallCaution:
                    screenHandle.DrawString(_smallFont, popup.InitialPos.Position, popup.Text, Color.Red);
                    break;
                case PopupType.Medium:
                    screenHandle.DrawString(_mediumFont, popup.InitialPos.Position, popup.Text, Color.LightGray);
                    break;
                case PopupType.MediumCaution:
                    screenHandle.DrawString(_mediumFont, popup.InitialPos.Position, popup.Text, Color.Red);
                    break;
                case PopupType.Large:
                    screenHandle.DrawString(_largeFont, popup.InitialPos.Position, popup.Text, Color.LightGray);
                    break;
                case PopupType.LargeCaution:
                    screenHandle.DrawString(_largeFont, popup.InitialPos.Position, popup.Text, Color.Red);
                    break;
                case PopupType.Small:
                default:
                    screenHandle.DrawString(_smallFont, popup.InitialPos.Position, popup.Text, Color.White);
                    break;
            }
        }
    }
}
