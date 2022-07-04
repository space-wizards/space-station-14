using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.MobState.Overlays;

public sealed class DamageOverlay : Overlay
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
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
        var viewport = args.WorldAABB;
        var worldHandle = args.WorldHandle;

        if (Dead)
        {
            ClearLerp();
            worldHandle.UseShader(_deadShader);
            worldHandle.DrawRect(viewport, Color.White);
            return;
        }

        switch (Level)
        {
            case 0:
                ClearLerp();
                break;
            default:
                double lerpRate = 0.1;
                var level = Level;
                float maxLevel = 2000f;
                float minLevel = 800f;

                if (!_oldlevel.Equals(Level))
                {
                    _lerpStart ??= _timing.RealTime;

                    var timeToLerp = (Level - _oldlevel) / lerpRate;
                    var currentRealTime = _timing.RealTime;
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

                var outerRadius = maxLevel - level * (maxLevel - minLevel);
                _damageShader.SetParameter("outerCircleRadius", outerRadius);
                _damageShader.SetParameter("innerCircleRadius", MathF.Min(200f, outerRadius - 400f));
                worldHandle.UseShader(_damageShader);
                worldHandle.DrawRect(viewport, Color.White);
                break;
        }

    }

    private void ClearLerp()
    {
        _oldlevel = Level;
        _lerpStart = null;
    }
}
