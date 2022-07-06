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

    private float _oldBruteLevel = 0f;

    /// <summary>
    /// Handles the darkening overlay.
    /// </summary>
    public float OxygenLevel = 0f;

    private float _oldOxygenLevel = 0f;

    /// <summary>
    /// Handles the white overlay when crit.
    /// </summary>
    public float CritLevel = 0f;

    private float _oldCritLevel = 0f;

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
        /*
         * Here's the rundown:
         * 1. There's lerping for each level so the transitions are smooth.
         * 2. There's 3 overlays, 1 for brute damage, 1 for oxygen damage (that also doubles as a crit overlay),
         * and a white one during crit that closes in as you progress towards death
         * The crit overlay also occasionally reduces its alpha as a "blink"
         */

        var viewport = args.ViewportBounds;
        var handle = args.ScreenHandle;
        var distance = args.ViewportBounds.Width;
        var lerpRate = 0.2f;

        switch (State)
        {
            case DamageState.Dead:
                _oldBruteLevel = BruteLevel;
                _oldCritLevel = CritLevel;
                _oldOxygenLevel = OxygenLevel;
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
        var lastFrameTime = (float) _timing.FrameTime.TotalSeconds;

        if (!_oldBruteLevel.Equals(BruteLevel))
        {
            var diff = BruteLevel - _oldBruteLevel;
            _oldBruteLevel += Math.Clamp(diff, -1 * lerpRate * lastFrameTime, lerpRate * lastFrameTime);
        }

        if (!_oldOxygenLevel.Equals(OxygenLevel))
        {
            var diff = OxygenLevel - _oldOxygenLevel;
            _oldOxygenLevel += Math.Clamp(diff, -1 * lerpRate * lastFrameTime, lerpRate * lastFrameTime);
        }

        if (!_oldCritLevel.Equals(CritLevel))
        {
            var diff = CritLevel - _oldCritLevel;
            _oldCritLevel += Math.Clamp(diff, -1 * lerpRate * lastFrameTime, lerpRate * lastFrameTime);
        }

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
        level = _oldBruteLevel;

        // TODO: Lerping
        if (level > 0f && _oldCritLevel <= 0f)
        {
            var pulseRate = 3f;
            var adjustedTime = time * pulseRate;
            float outerMaxLevel = 2.0f * distance;
            float outerMinLevel = 0.8f * distance;
            float innerMaxLevel = 0.6f * distance;
            float innerMinLevel = 0.2f * distance;

            var outerRadius = outerMaxLevel - level * (outerMaxLevel - outerMinLevel);
            var innerRadius = innerMaxLevel - level * (innerMaxLevel - innerMinLevel);

            var pulse = MathF.Max(0f, MathF.Sin(adjustedTime));

            _bruteShader.SetParameter("time", pulse);
            _bruteShader.SetParameter("color", new Vector3(1f, 0f, 0f));
            _bruteShader.SetParameter("darknessAlphaOuter", 0.8f);

            _bruteShader.SetParameter("outerCircleRadius", outerRadius);
            _bruteShader.SetParameter("outerCircleMaxRadius", outerRadius + 0.2f * distance);
            _bruteShader.SetParameter("innerCircleRadius", innerRadius);
            _bruteShader.SetParameter("innerCircleMaxRadius", innerRadius + 0.02f * distance);
            handle.UseShader(_bruteShader);
            handle.DrawRect(viewport, Color.White);
        }
        else
        {
            _oldBruteLevel = BruteLevel;
        }

        level = State != DamageState.Critical ? _oldOxygenLevel : 1f;

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
            if (_oldCritLevel > 0f)
            {
                var adjustedTime = time * 2f;
                critTime = MathF.Max(0, MathF.Sin(adjustedTime) + 2 * MathF.Sin(2 * adjustedTime / 4f) + MathF.Sin(adjustedTime / 4f) - 3f);

                if (critTime > 0f)
                {
                    outerDarkness = 1f - critTime / 1.5f;
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
            _oxygenShader.SetParameter("color", new Vector3(0f, 0f, 0f));
            _oxygenShader.SetParameter("darknessAlphaOuter", outerDarkness);
            _oxygenShader.SetParameter("innerCircleRadius", innerRadius);
            _oxygenShader.SetParameter("innerCircleMaxRadius", innerRadius);
            _oxygenShader.SetParameter("outerCircleRadius", outerRadius);
            _oxygenShader.SetParameter("outerCircleMaxRadius", outerRadius + 0.2f * distance);
            handle.UseShader(_oxygenShader);
            handle.DrawRect(viewport, Color.White);
        }

        level = _oldCritLevel;

        if (level > 0f)
        {
            float outerMaxLevel = 2.0f * distance;
            float outerMinLevel = 1.0f * distance;
            float innerMaxLevel = 0.6f * distance;
            float innerMinLevel = 0.02f * distance;

            var outerRadius = outerMaxLevel - level * (outerMaxLevel - outerMinLevel);
            var innerRadius = innerMaxLevel - level * (innerMaxLevel - innerMinLevel);

            var pulse = MathF.Max(0f, MathF.Sin(time));

            // If in crit then just fix it; also pulse it very occasionally so they can see more.
            _critShader.SetParameter("time", pulse);
            _critShader.SetParameter("color", new Vector3(1f, 1f, 1f));
            _critShader.SetParameter("darknessAlphaOuter", 1.0f);
            _critShader.SetParameter("innerCircleRadius", innerRadius);
            _critShader.SetParameter("innerCircleMaxRadius", innerRadius + 0.005f * distance);
            _critShader.SetParameter("outerCircleRadius", outerRadius);
            _critShader.SetParameter("outerCircleMaxRadius", outerRadius + 0.2f * distance);
            handle.UseShader(_critShader);
            handle.DrawRect(viewport, Color.White);
        }
    }
}
