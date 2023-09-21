using System.Numerics;
using Content.Client.Viewport;
using Content.Shared.CCVar;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Graphics;

namespace Content.Client.UserInterface.Controls
{
    /// <summary>
    ///     Wrapper for <see cref="ScalingViewport"/> that listens to configuration variables.
    ///     Also does NN-snapping within tolerances.
    /// </summary>
    [Virtual]
    public class MainViewport : UIWidget
    {
        [Dependency] protected readonly IConfigurationManager CfgMan = default!;
        [Dependency] private readonly ViewportManager _vpManager = default!;

        public ScalingViewport Viewport;

        public MainViewport()
        {
            LayoutContainer.SetAnchorPreset(this, LayoutContainer.LayoutPreset.Wide);

            Viewport = new ScalingViewport
            {
                HorizontalExpand = true,
                VerticalExpand = true,
                AlwaysRender = true,
                RenderScaleMode = ScalingViewportRenderScaleMode.CeilInt,
                MouseFilter = MouseFilterMode.Stop
            };

            AddChild(Viewport);
        }

        public virtual IEye Eye
        {
            set => Viewport.Eye = value;
        }

        public virtual Vector2i ViewportSize
        {
            set => Viewport.ViewportSize = value;
        }

        protected override void EnteredTree()
        {
            base.EnteredTree();

            _vpManager.AddViewport(this);
        }

        protected override void ExitedTree()
        {
            base.ExitedTree();

            _vpManager.RemoveViewport(this);
        }

        public void UpdateCfg(ScalingViewport viewport)
        {
            var stretch = CfgMan.GetCVar(CCVars.ViewportStretch);
            var renderScaleUp = CfgMan.GetCVar(CCVars.ViewportScaleRender);
            var fixedFactor = CfgMan.GetCVar(CCVars.ViewportFixedScaleFactor);

            if (stretch)
            {
                var snapFactor = CalcSnappingFactor(viewport);
                if (snapFactor == null)
                {
                    // Did not find a snap, enable stretching.
                    viewport.FixedStretchSize = null;
                    viewport.StretchMode = ScalingViewportStretchMode.Bilinear;
                    viewport.FixedStretchSize = null;
                    viewport.StretchMode = ScalingViewportStretchMode.Bilinear;

                    if (renderScaleUp)
                    {
                        viewport.RenderScaleMode = ScalingViewportRenderScaleMode.CeilInt;
                    }
                    else
                    {
                        viewport.RenderScaleMode = ScalingViewportRenderScaleMode.Fixed;
                        viewport.FixedRenderScale = 1;
                    }

                    return;
                }

                // Found snap, set fixed factor and run non-stretching code.
                fixedFactor = snapFactor.Value;
            }

            viewport.FixedStretchSize = viewport.ViewportSize * fixedFactor;
            viewport.StretchMode = ScalingViewportStretchMode.Nearest;

            if (renderScaleUp)
            {
                viewport.RenderScaleMode = ScalingViewportRenderScaleMode.Fixed;
                viewport.FixedRenderScale = fixedFactor;
            }
            else
            {
                // Snapping but forced to render scale at scale 1 so...
                // At least we can NN.
                viewport.RenderScaleMode = ScalingViewportRenderScaleMode.Fixed;
                viewport.FixedRenderScale = 1;
            }
        }

        private int? CalcSnappingFactor(ScalingViewport viewport)
        {
            // Margin tolerance is tolerance of "the window is too big"
            // where we add a margin to the viewport to make it fit.
            var cfgToleranceMargin = CfgMan.GetCVar(CCVars.ViewportSnapToleranceMargin);
            // Clip tolerance is tolerance of "the window is too small"
            // where we are clipping the viewport to make it fit.
            var cfgToleranceClip = CfgMan.GetCVar(CCVars.ViewportSnapToleranceClip);

            // Calculate if the viewport, when rendered at an integer scale,
            // is close enough to the control size to enable "snapping" to NN,
            // potentially cutting a tiny bit off/leaving a margin.
            //
            // Idea here is that if you maximize the window at 1080p or 1440p
            // we are close enough to an integer scale (2x and 3x resp) that we should "snap" to it.

            // Just do it iteratively.
            // I'm sure there's a smarter approach that needs one try with math but I'm dumb.
            for (var i = 1; i <= 10; i++)
            {
                var toleranceMargin = i * cfgToleranceMargin;
                var toleranceClip = i * cfgToleranceClip;
                var scaled = (Vector2) viewport.ViewportSize * i;
                var (dx, dy) = PixelSize - scaled;

                // The rule for which snap fits is that at LEAST one axis needs to be in the tolerance size wise.
                // One axis MAY be larger but not smaller than tolerance.
                // Obviously if it's too small it's bad, and if it's too big on both axis we should stretch up.
                if (Fits(dx) && Fits(dy) || Fits(dx) && Larger(dy) || Larger(dx) && Fits(dy))
                {
                    // Found snap that fits.
                    return i;
                }

                bool Larger(float a)
                {
                    return a > toleranceMargin;
                }

                bool Fits(float a)
                {
                    return a <= toleranceMargin && a >= -toleranceClip;
                }
            }

            return null;
        }

        protected override void Resized()
        {
            base.Resized();

            UpdateCfg();
        }

        public virtual void UpdateCfg()
        {
            UpdateCfg(Viewport);
        }
    }
}
