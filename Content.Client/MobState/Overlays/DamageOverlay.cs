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
    private readonly ShaderInstance _damageShader;

    public float Level;
    private float _oldlevel;

    public bool Dead = false;

    private TimeSpan? _lerpStart;

    public DamageOverlay()
    {
        // TODO: Replace
        IoCManager.InjectDependencies(this);
        _deadShader = _prototypeManager.Index<ShaderPrototype>("CircleMask").Instance();
        _damageShader = _prototypeManager.Index<ShaderPrototype>("DamageMask").InstanceUnique();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var viewport = args.ViewportBounds;
        var handle = args.ScreenHandle;

        if (Dead)
        {
            ClearLerp();
            handle.UseShader(_deadShader);
            handle.DrawRect(viewport, Color.White);
            return;
        }

        switch (Level)
        {
            case 0:
                ClearLerp();
                break;
            default:
                double lerpRate = 0.25;
                var level = Level;
                var distance = args.ViewportBounds.Width;

                float outerMaxLevel = 1.6f * distance;
                float outerMinLevel = 0.8f * distance;
                float innerMaxLevel = 0.5f * distance;
                float innerMinLevel = 0.25f * distance;
                var currentRealTime = _timing.RealTime;

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
                        level = (float) (_oldlevel + Math.Min(1, lerpDifference / timeToLerp) * (Level - _oldlevel));
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
