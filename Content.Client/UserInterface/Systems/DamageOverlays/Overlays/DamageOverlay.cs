using Content.Client.DeadSpace.Heartbeat;
using Content.Shared.Mobs;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface.Systems.DamageOverlays.Overlays;

public sealed class DamageOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> DamageShader = "DeadSpaceDamageBurn";

    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    public MobState State = MobState.Alive;
    public float PainLevel;
    public float OxygenLevel;
    public float PreCriticalLevel;
    public float CritLevel;
    public float DeadLevel = 1f;

    private readonly ShaderInstance _shader;
    private CritHeartbeatSystem? _heartbeat;
    private float _pain;
    private float _oxygen;
    private float _preCritical;
    private float _critical;
    private float _preCriticalState;
    private float _criticalState;
    private float _deadState;

    public DamageOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototypeManager.Index(DamageShader).InstanceUnique();
    }

    public void Reset()
    {
        State = MobState.Alive;
        PainLevel = 0f;
        OxygenLevel = 0f;
        PreCriticalLevel = 0f;
        CritLevel = 0f;
        DeadLevel = 0f;
        _pain = 0f;
        _oxygen = 0f;
        _preCritical = 0f;
        _critical = 0f;
        _preCriticalState = 0f;
        _criticalState = 0f;
        _deadState = 0f;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null ||
            !_entityManager.TryGetComponent(_playerManager.LocalEntity, out EyeComponent? eye) ||
            args.Viewport.Eye != eye.Eye)
        {
            return;
        }

        var frameTime = (float) _timing.FrameTime.TotalSeconds;
        _pain = Approach(_pain, PainLevel, frameTime, 3.4f);
        _oxygen = Approach(_oxygen, OxygenLevel, frameTime, 3.4f);
        _preCritical = Approach(_preCritical, PreCriticalLevel, frameTime, 3f);
        _critical = Approach(_critical, CritLevel, frameTime, 2.8f);

        var preCriticalTarget = State == MobState.PreCritical ? 1f : 0f;
        var criticalTarget = State == MobState.Critical ? 1f : 0f;
        var deadTarget = State == MobState.Dead ? 1f : 0f;
        _preCriticalState = Approach(_preCriticalState, preCriticalTarget, frameTime, 2.8f);
        _criticalState = Approach(_criticalState, criticalTarget, frameTime, 2.8f);
        _deadState = Approach(_deadState, deadTarget, frameTime, 2.8f);

        if (State != MobState.Dead)
            DeadLevel = 1f;
        else
            DeadLevel = Approach(DeadLevel, 0f, frameTime, 1.35f);

        if (_pain <= 0f &&
            _oxygen <= 0f &&
            _preCriticalState <= 0f &&
            _criticalState <= 0f &&
            _deadState <= 0f)
        {
            return;
        }

        if (_heartbeat == null)
            _entitySystemManager.TryGetEntitySystem(out _heartbeat);

        _shader.SetParameter("DamageLevel", Math.Clamp(_pain, 0f, 1f));
        _shader.SetParameter("OxygenLevel", Math.Clamp(_oxygen, 0f, 1f));
        _shader.SetParameter("PreCriticalLevel", Math.Clamp(_preCritical, 0f, 1f));
        _shader.SetParameter("CriticalLevel", Math.Clamp(_critical, 0f, 1f));
        _shader.SetParameter("PreCriticalState", _preCriticalState);
        _shader.SetParameter("CriticalState", _criticalState);
        _shader.SetParameter("DeadState", _deadState);
        _shader.SetParameter("DeathFade", DeadLevel);
        _shader.SetParameter("Pulse", Math.Clamp(_heartbeat?.VisualPulse ?? 0f, 0f, 1f));
        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);

        var handle = args.WorldHandle;
        handle.UseShader(_shader);
        handle.DrawRect(args.WorldAABB, Color.White);
        handle.UseShader(null);
    }

    private static float Approach(float current, float target, float frameTime, float speed)
    {
        var difference = target - current;
        if (MathHelper.CloseTo(difference, 0f, 0.001f))
            return target;

        return current + difference * Math.Clamp(speed * frameTime, 0f, 1f);
    }
}
