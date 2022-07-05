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
    private readonly ShaderInstance _deadShader;
    private readonly ShaderInstance _critShader;
    private readonly ShaderInstance _damageShader;

    public DamageState State = DamageState.Alive;

    /// <summary>
    /// Current level for the radius from 0 -> 1
    /// </summary>
    public float Level;

    /// <summary>
    /// Used for lerping.
    /// </summary>
    private float _oldlevel;

    public float 

    private TimeSpan? _lerpStart;

    public DamageOverlay()
    {
        // TODO: Replace
        IoCManager.InjectDependencies(this);
        _deadShader = _prototypeManager.Index<ShaderPrototype>("CircleMask").Instance();
        _critShader = _prototypeManager.Index<ShaderPrototype>("GradientCircleMask").Instance();
        _damageShader = _prototypeManager.Index<ShaderPrototype>("DamageMask").InstanceUnique();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var viewport = args.ViewportBounds;
        var handle = args.ScreenHandle;

        switch (State)
        {
            case DamageState.Dead:
                ClearLerp();
                handle.UseShader(_deadShader);
                handle.DrawRect(viewport, Color.White);
                return;
            case DamageState.Critical:
                ClearLerp();
                handle.UseShader(_critShader);
                handle.DrawRect(viewport, Color.White);
                return;
            case DamageState.Alive:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        // I just didn't want all of alive indented
        switch (Level)
        {
            case 0:
                ClearLerp();
                break;
            default:
                double lerpRate = 0.5;
                var level = Level;
                var distance = args.ViewportBounds.Width;

                float outerMaxLevel = 1.6f * distance;
                float outerMinLevel = 0.8f * distance;
                float innerMaxLevel = 0.5f * distance;
                float innerMinLevel = 0.1f * distance;
                var currentRealTime = _timing.RealTime;

                // TODO: This is still kinda jank lerping for heals.
                if (!_oldlevel.Equals(Level))
                {
                    _lerpStart ??= _timing.RealTime;

                    var timeToLerp = (Level - _oldlevel) / lerpRate;
                    var lerpDifference = (currentRealTime - _lerpStart.Value).TotalSeconds;

                    // Lerp time has elapsed so end it.
                    if (lerpDifference > Math.Abs(timeToLerp))
                    {
                        ClearLerp();
                    }
                    else
                    {
                        level = (float) (_oldlevel + Math.Clamp(lerpDifference / timeToLerp, -1, 1) * (Level - _oldlevel));
                    }
                }

                var outerRadius = outerMaxLevel - level * (outerMaxLevel - outerMinLevel);
                var innerRadius = innerMaxLevel - level * (innerMaxLevel - innerMinLevel);
                _damageShader.SetParameter("time", (float) currentRealTime.TotalSeconds);

                _damageShader.SetParameter("outerCircleRadius", outerRadius);
                _damageShader.SetParameter("outerCircleMaxRadius", outerRadius + 0.2f * distance);
                _damageShader.SetParameter("innerCircleRadius", innerRadius);
                _damageShader.SetParameter("innerCircleMaxRadius", innerRadius + 0.02f * distance);
                handle.UseShader(_damageShader);
                handle.DrawRect(viewport, Color.White);
                break;
        }
    }

    private void ClearLerp()
    {
        _oldlevel = Level;
        _lerpStart = null;
    }
}
