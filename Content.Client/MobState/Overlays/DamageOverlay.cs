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

    public int Level { get; set; }
    private int _oldlevel;

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

        switch (Level)
        {
            case 0:
                ClearLerp();
                break;
            case MobStateSystem.Levels:
                ClearLerp();
                worldHandle.UseShader(_deadShader);
                worldHandle.DrawRect(viewport, Color.White);
                break;
            default:
                double lerpRate = 2;
                var level = (float) Level;
                float maxLevel = 800f;
                float minLevel = 80f;

                if (_oldlevel != Level)
                {
                    _lerpStart ??= _timing.RealTime;

                    var timeToLerp = lerpRate / (Level - _oldlevel);
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

                _damageShader.SetParameter("outerCircleRadius", maxLevel - (level / (MobStateSystem.Levels - 1)) * (maxLevel - minLevel));
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
