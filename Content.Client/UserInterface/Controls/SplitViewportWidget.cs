using Content.Client.Viewport;
using Robust.Client.UserInterface.Controls;
using Robust.Shared;
using Robust.Shared.Graphics;

namespace Content.Client.UserInterface.Controls;

/// <summary>
/// Variant of <see cref="MainViewport"/> that draws two separate viewports. This is intended to make it easy to
/// compare different rendering options/cvars by changing options in <see cref="BeforeDrawMain"/> and <see cref="BeforeDrawAlt"/>
/// </summary>
public sealed class SplitViewportWidget : MainViewport
{
    public ScalingViewport AltViewport;
    public SplitContainer SplitContainer;

    public SplitViewportWidget()
    {
        AltViewport = new ScalingViewport
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            AlwaysRender = true,
            RenderScaleMode = ScalingViewportRenderScaleMode.CeilInt,
            MouseFilter = MouseFilterMode.Stop,
            Eye = Viewport.Eye,
            ViewportSize = Viewport.ViewportSize,
        };

        SplitContainer = new SplitContainer
        {
            VerticalExpand = true,
            HorizontalExpand = true,
            SplitWidth = 4
        };

        RemoveChild(Viewport);
        SplitContainer.AddChild(Viewport);
        SplitContainer.AddChild(AltViewport);
        AddChild(SplitContainer);

        Viewport.SizeOverride += GetSizeOverride;
        Viewport.GlobalPositionOverride +=  GlobalPositionOverride;
        Viewport.BeforeDraw += BeforeDrawMain;

        AltViewport.SizeOverride += GetSizeOverride;
        AltViewport.GlobalPositionOverride += GlobalPositionOverride;
        AltViewport.BeforeDraw += BeforeDrawAlt;
    }

    private Vector2i GetSizeOverride() => SplitContainer?.PixelSize ?? default;
    private Vector2i GlobalPositionOverride() => SplitContainer?.GlobalPixelPosition ?? default;

    public override IEye Eye
    {
        set
        {
            Viewport.Eye = value;
            AltViewport.Eye = value;
        }
    }

    public override Vector2i ViewportSize
    {
        set
        {
            Viewport.ViewportSize = value;
            AltViewport.ViewportSize = value;
        }
    }

    public override void UpdateCfg()
    {
        UpdateCfg(Viewport);
        UpdateCfg(AltViewport);
    }

    private void BeforeDrawMain()
    {
        // Configure rendering for the primary viewport.
        // E.g., compare soft & hard lighting

        // Note that some options like light resolution scale are applied globally across all viewports, and cannot
        // be changed in the middle of rendering.

        CfgMan.SetCVar(CVars.LightSoftShadows, true);
        CfgMan.SetCVar(CVars.LightBlur, true);
    }

    private void BeforeDrawAlt()
    {
        CfgMan.SetCVar(CVars.LightSoftShadows, false);
        CfgMan.SetCVar(CVars.LightBlur, false);
    }
}