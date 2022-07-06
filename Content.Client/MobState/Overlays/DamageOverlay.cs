using Content.Shared.MobState;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.MobState.Overlays;

public sealed class DamageOverlay : Overlay
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    private readonly ShaderInstance _critShader;
    private readonly ShaderInstance _deadShader;
    private readonly ShaderInstance _oxygenShader;
    private readonly ShaderInstance _bruteShader;

    public DamageState State = DamageState.Alive;

    /// <summary>
    /// Handles the red pulsing overlay
    /// </summary>
    public float BruteLevel = 0f;

    /// <summary>
    /// Handles the darkening overlay.
    /// </summary>
    public float OxygenLevel = 0f;

    /// <summary>
    /// Handles the white overlay when crit.
    /// </summary>
    public float CritLevel = 0f;

    public DamageOverlay()
    {
        // TODO: Replace
        IoCManager.InjectDependencies(this);
        _deadShader = _prototypeManager.Index<ShaderPrototype>("CircleMask").Instance();
        _oxygenShader = _prototypeManager.Index<ShaderPrototype>("GradientCircleMask").InstanceUnique();
        _critShader = _prototypeManager.Index<ShaderPrototype>("GradientCircleMask").InstanceUnique();
        _bruteShader = _prototypeManager.Index<ShaderPrototype>("GradientCircleMask").InstanceUnique();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var viewport = args.ViewportBounds;
        var handle = args.ScreenHandle;
        var distance = args.ViewportBounds.Width;

        switch (State)
        {
            case DamageState.Dead:
                handle.UseShader(_deadShader);
                handle.DrawRect(viewport, Color.White);
                return;
           case DamageState.Critical:
           case DamageState.Alive:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        var time = (float) _timing.RealTime.TotalSeconds;

        /*
         * darknessAlphaOuter is the maximum alpha for anything outside of the larger circle
         * darknessAlphaInner (on the shader) is the alpha for anything inside the smallest circle
         *
         * outerCircleRadius is what we end at for max level for the outer circle
         * outerCircleMaxRadius is what we start at for 0 level for the outer circle
         *
         * innerCircleRadius is what we end at for max level for the inner circle
         * innerCircleMaxRadius is what we start at for 0 level for the inner circle
         */

        // Makes debugging easier don't @ me
        float level = 0f;
        level = BruteLevel;
        // level = 0f;

        // TODO: Lerping
        if (level > 0f && CritLevel <= 0f)
        {
            var pulseRate = 3f;
            var adjustedTime = time * pulseRate;
            float outerMaxLevel = 2.0f * distance;
            float outerMinLevel = 0.8f * distance;
            float innerMaxLevel = 0.6f * distance;
            float innerMinLevel = 0.2f * distance;

            var outerRadius = outerMaxLevel - BruteLevel * (outerMaxLevel - outerMinLevel);
            var innerRadius = innerMaxLevel - BruteLevel * (innerMaxLevel - innerMinLevel);

            var pulse = MathF.Max(0f, MathF.Sin(adjustedTime));

            _bruteShader.SetParameter("time", pulse);
            _bruteShader.SetParameter("colorX", 1.0f);
            _bruteShader.SetParameter("darknessAlphaOuter", 0.8f);

            _bruteShader.SetParameter("outerCircleRadius", outerRadius);
            _bruteShader.SetParameter("outerCircleMaxRadius", outerRadius + 0.2f * distance);
            _bruteShader.SetParameter("innerCircleRadius", innerRadius);
            _bruteShader.SetParameter("innerCircleMaxRadius", innerRadius + 0.02f * distance);
            handle.UseShader(_bruteShader);
            handle.DrawRect(viewport, Color.White);
        }

        level = CritLevel == 0f ? OxygenLevel : 1f;

        if (level > 0f)
        {
            float outerMaxLevel = 0.6f * distance;
            float outerMinLevel = 0.06f * distance;
            float innerMaxLevel = 0.02f * distance;
            float innerMinLevel = 0.02f * distance;

            var outerRadius = outerMaxLevel - level * (outerMaxLevel - outerMinLevel);
            var innerRadius = innerMaxLevel - level * (innerMaxLevel - innerMinLevel);

            float outerDarkness;
            float critTime;

            // If in crit then just fix it; also pulse it very occasionally so they can see more.
            if (CritLevel > 0f)
            {
                var adjustedTime = time * 1.5f;
                critTime = MathF.Max(0f, MathF.Cos(adjustedTime / 2f) - 1f + MathF.Sin(adjustedTime));

                if (critTime > 0f)
                {
                    outerDarkness = 1f - critTime / 2f;
                }
                else
                {
                    outerDarkness = 1f;
                }
            }
            else
            {
                outerDarkness = MathF.Min(0.98f, 0.8f * MathF.Log(level) + 1f);
            }

            _oxygenShader.SetParameter("time", 0.0f);
            _oxygenShader.SetParameter("colorX", 0.0f);
            _oxygenShader.SetParameter("darknessAlphaOuter", outerDarkness);
            _oxygenShader.SetParameter("innerCircleRadius", innerRadius);
            _oxygenShader.SetParameter("innerCircleMaxRadius", innerRadius);
            _oxygenShader.SetParameter("outerCircleRadius", outerRadius);
            _oxygenShader.SetParameter("outerCircleMaxRadius", outerRadius + 0.2f * distance);
            handle.UseShader(_oxygenShader);
            handle.DrawRect(viewport, Color.White);
        }

        level = CritLevel;
        level = 0f;

        if (level > 0f)
        {
            _critShader.SetParameter("darknessAlphaInner", 0.6f);
            _critShader.SetParameter("darknessAlphaOuter", 0.9f);
            _critShader.SetParameter("innerCircleRadius", 40.0f);
            _critShader.SetParameter("outerCircleRadius", 80.0f);
            handle.UseShader(_critShader);
            handle.DrawRect(viewport, Color.White);
        }
    }
}
