using Content.Shared.Mobs;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface.Systems.DamageOverlays.Overlays;

public sealed class DamageOverlay : Overlay
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly ShaderInstance _critShader;
    private readonly ShaderInstance _oxygenShader;
    private readonly ShaderInstance _bruteShader;

    public MobState State = MobState.Alive;

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

    public float DeadLevel = 1f;

    public DamageOverlay()
    {
        // TODO: Replace
        IoCManager.InjectDependencies(this);
        _oxygenShader = _prototypeManager.Index<ShaderPrototype>("GradientCircleMask").InstanceUnique();
        _critShader = _prototypeManager.Index<ShaderPrototype>("GradientCircleMask").InstanceUnique();
        _bruteShader = _prototypeManager.Index<ShaderPrototype>("GradientCircleMask").InstanceUnique();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_entityManager.TryGetComponent(_playerManager.LocalPlayer?.ControlledEntity, out EyeComponent? eyeComp))
            return;

        if (args.Viewport.Eye != eyeComp.Eye)
            return;

        /*
         * Here's the rundown:
         * 1. There's lerping for each level so the transitions are smooth.
         * 2. There's 3 overlays, 1 for brute damage, 1 for oxygen damage (that also doubles as a crit overlay),
         * and a white one during crit that closes in as you progress towards death. When you die it slowly disappears.
         * The crit overlay also occasionally reduces its alpha as a "blink"
         */

        var viewport = args.WorldAABB;
        var handle = args.WorldHandle;
        var distance = args.ViewportBounds.Width;

        var time = (float) _timing.RealTime.TotalSeconds;
        var lastFrameTime = (float) _timing.FrameTime.TotalSeconds;

        // If they just died then lerp out the white overlay.
        if (State != MobState.Dead)
        {
            DeadLevel = 1f;
        }
        else if (!MathHelper.CloseTo(0f, DeadLevel, 0.001f))
        {
            var diff = -DeadLevel;
            DeadLevel += GetDiff(diff, lastFrameTime);
        }
        else
        {
            DeadLevel = 0f;
        }

        if (!MathHelper.CloseTo(_oldBruteLevel, BruteLevel, 0.001f))
        {
            var diff = BruteLevel - _oldBruteLevel;
            _oldBruteLevel += GetDiff(diff, lastFrameTime);
        }
        else
        {
            _oldBruteLevel = BruteLevel;
        }

        if (!MathHelper.CloseTo(_oldOxygenLevel, OxygenLevel, 0.001f))
        {
            var diff = OxygenLevel - _oldOxygenLevel;
            _oldOxygenLevel += GetDiff(diff, lastFrameTime);
        }
        else
        {
            _oldOxygenLevel = OxygenLevel;
        }

        if (!MathHelper.CloseTo(_oldCritLevel, CritLevel, 0.001f))
        {
            var diff = CritLevel - _oldCritLevel;
            _oldCritLevel += GetDiff(diff, lastFrameTime);
        }
        else
        {
            _oldCritLevel = CritLevel;
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

        level = State != MobState.Critical ? _oldOxygenLevel : 1f;

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
                outerDarkness = MathF.Min(0.98f, 0.3f * MathF.Log(level) + 1f);
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

        level = State != MobState.Dead ? _oldCritLevel : DeadLevel;

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

        handle.UseShader(null);
    }

    private float GetDiff(float value, float lastFrameTime)
    {
        var adjustment = value * 5f * lastFrameTime;

        if (value < 0f)
            adjustment = Math.Clamp(adjustment, value, -value);
        else
            adjustment = Math.Clamp(adjustment, -value, value);

        return adjustment;
    }
}
