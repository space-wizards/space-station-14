using System;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Content.Client.Stylesheets;
using Robust.Client.UserInterface;
using Robust.Shared.Enums;
using Robust.Shared.Timing;
using Content.Client.GameTicking.Managers;
using Robust.Shared.GameObjects;

namespace Content.Client.Overlays;

public sealed class BodycamHudOverlay : Overlay
{
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IUserInterfaceManager _uiMan = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;

    private Font _font = default!;
    private Font _fontBold = default!;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    // HUD options
    public bool ShowRec { get; set; } = true;
    public bool ShowTimestamp { get; set; } = true;
    public bool ShowFrame { get; set; } = true;

    public BodycamHudOverlay()
    {
        IoCManager.InjectDependencies(this);
        _font = _resourceCache.NotoStack();
        _fontBold = _resourceCache.NotoStack(variation: "Bold");
        ZIndex = 150; // above world post-process, below most UI
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.ScreenHandle;
        var uiScale = _uiMan.RootControl.UIScale;
        var vp = args.ViewportBounds;

        // REC indicator (top-right) and timestamp below it. Both at 2x size.
        Vector2? recRightEdge = null;
        Vector2? recTextPosCache = null;
        float recScale = uiScale * 2f;
        if (ShowRec)
        {
            var t = (float) _timing.CurTime.TotalSeconds;
            var blink = ((int) Math.Floor(t % 1.0f * 2)) % 2 == 0; // 2Hz blink
            var color = blink ? Color.Red : new Color(0.5f, 0.0f, 0.0f, 1f);

            var recText = "REC";
            var recSize = handle.GetDimensions(_fontBold, recText, recScale);
            var recTextPos = new Vector2(vp.Right - 12f * uiScale - recSize.X, vp.Top + 10f * uiScale);
            // Dot to the left of text, vertically centered (scaled with size)
            var dotRadius = 12f * uiScale;
            var dotCenter = new Vector2(recTextPos.X - 18f * uiScale, recTextPos.Y + recSize.Y * 0.5f);

            handle.DrawCircle(dotCenter, dotRadius, color);
            handle.DrawString(_fontBold, recTextPos, recText, recScale, Color.White);

            recRightEdge = new Vector2(recTextPos.X + recSize.X, recTextPos.Y);
            recTextPosCache = recTextPos;
        }

        // Timestamp (place slightly below REC, 2x size, right-aligned to REC)
        if (ShowTimestamp)
        {
            // Use round elapsed time from the client ticker for syncing with the round clock
            var ts = _entMan.System<ClientGameTicker>().RoundDuration();
            ts = TimeSpan.FromSeconds(Math.Floor(ts.TotalSeconds));
            var text = $"{(int)ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
            var tsScale = uiScale * 2f;
            var tsSize = handle.GetDimensions(_font, text, tsScale);

            // Default to aligning to viewport right if REC hidden
            Vector2 baseRight;
            if (recRightEdge != null && recTextPosCache != null)
            {
                baseRight = recRightEdge.Value;
                var gapY = 4f * uiScale;
                var pos = new Vector2(baseRight.X - tsSize.X, recTextPosCache.Value.Y + handle.GetDimensions(_fontBold, "REC", recScale).Y + gapY);
                handle.DrawString(_font, pos, text, tsScale, Color.White);
            }
            else
            {
                baseRight = new Vector2(vp.Right - 12f * uiScale, vp.Top + 10f * uiScale);
                var pos = new Vector2(baseRight.X - tsSize.X, baseRight.Y + 20f * uiScale);
                handle.DrawString(_font, pos, text, tsScale, Color.White);
            }
        }

        // Frame corners
        if (ShowFrame)
        {
            var margin = 8f * uiScale;
            var len = 26f * uiScale;
            var thick = 2f * uiScale;

            // Top-left
            DrawL(handle, new Vector2(vp.Left + margin, vp.Top + margin), len, thick, true, true);
            // Top-right
            DrawL(handle, new Vector2(vp.Right - margin, vp.Top + margin), len, thick, false, true);
            // Bottom-left
            DrawL(handle, new Vector2(vp.Left + margin, vp.Bottom - margin), len, thick, true, false);
            // Bottom-right
            DrawL(handle, new Vector2(vp.Right - margin, vp.Bottom - margin), len, thick, false, false);
        }
    }

    private void DrawL(DrawingHandleScreen handle, Vector2 corner, float len, float thick, bool left, bool top)
    {
        var color = new Color(1f, 1f, 1f, 0.7f);
        // Horizontal
        var hA = corner + new Vector2(left ? 0 : -len, 0);
        var hB = corner;
        var vA = corner + new Vector2(0, top ? 0 : -len);
        var vB = corner;
        handle.DrawLine(hA, hB, color);
        handle.DrawLine(vA, vB, color);
    }
}
