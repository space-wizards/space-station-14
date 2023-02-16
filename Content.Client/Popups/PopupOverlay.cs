using Content.Client.Examine;
using Content.Shared.CCVar;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.Popups;

/// <summary>
/// Draws popup text, either in world or on screen.
/// </summary>
public sealed class PopupOverlay : Overlay
{
    private readonly IConfigurationManager _configManager;
    private readonly IEntityManager _entManager;
    private readonly IPlayerManager _playerMgr;
    private readonly IUserInterfaceManager _uiManager;
    private readonly PopupSystem _popup;

    private readonly ShaderInstance _shader;
    private readonly Font _smallFont;
    private readonly Font _mediumFont;
    private readonly Font _largeFont;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public PopupOverlay(
        IConfigurationManager configManager,
        IEntityManager entManager,
        IPlayerManager playerMgr,
        IPrototypeManager protoManager,
        IResourceCache cache,
        IUserInterfaceManager uiManager,
        PopupSystem popup)
    {
        _configManager = configManager;
        _entManager = entManager;
        _playerMgr = playerMgr;
        _uiManager = uiManager;
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
        var scale = _configManager.GetCVar(CVars.DisplayUIScale);

        if (scale == 0f)
            scale = _uiManager.DefaultUIScale;

        DrawWorld(args.ScreenHandle, args, scale);
        DrawScreen(args.ScreenHandle, args, scale);

        args.DrawingHandle.UseShader(null);
    }

    private void DrawWorld(DrawingHandleScreen worldHandle, OverlayDrawArgs args, float scale)
    {
        if (_popup.WorldLabels.Count == 0)
            return;

        var matrix = args.ViewportControl!.GetWorldToScreenMatrix();
        var viewPos = new MapCoordinates(args.WorldAABB.Center, args.MapId);
        var ourEntity = _playerMgr.LocalPlayer?.ControlledEntity;

        foreach (var popup in _popup.WorldLabels)
        {
            var mapPos = popup.InitialPos.ToMap(_entManager);

            if (mapPos.MapId != args.MapId)
                continue;

            var distance = (mapPos.Position - args.WorldBounds.Center).Length;

            // Should handle fade here too wyci.
            if (!args.WorldAABB.Contains(mapPos.Position) || !ExamineSystemShared.InRangeUnOccluded(viewPos, mapPos, distance,
                    e => e == popup.InitialPos.EntityId || e == ourEntity, entMan: _entManager))
                continue;

            var pos = matrix.Transform(mapPos.Position);
            DrawPopup(popup, worldHandle, pos, scale);
        }
    }

    private void DrawScreen(DrawingHandleScreen screenHandle, OverlayDrawArgs args, float scale)
    {
        foreach (var popup in _popup.CursorLabels)
        {
            // Different window
            if (popup.InitialPos.Window != args.ViewportControl?.Window?.Id)
                continue;

            DrawPopup(popup, screenHandle, popup.InitialPos.Position, scale);
        }
    }

    private void DrawPopup(PopupSystem.PopupLabel popup, DrawingHandleScreen handle, Vector2 position, float scale)
    {
        const float alphaMinimum = 0.5f;

        var alpha = MathF.Min(1f, 1f - (popup.TotalTime - alphaMinimum) / (PopupSystem.PopupLifetime - alphaMinimum));
        var updatedPosition = position - new Vector2(0f, 20f * (popup.TotalTime * popup.TotalTime + popup.TotalTime));
        var font = _smallFont;
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

        var dimensions = handle.GetDimensions(font, popup.Text, scale);
        handle.DrawString(font, updatedPosition - dimensions / 2f, popup.Text, scale, color.WithAlpha(alpha));
    }
}
