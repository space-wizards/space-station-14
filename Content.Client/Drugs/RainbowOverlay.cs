using Content.Shared.CCVar;
using Content.Shared.Drugs;
using Content.Shared.StatusEffectNew;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Drugs;

public sealed class RainbowOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> Shader = "Rainbow";

    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntitySystemManager _sysMan = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    private readonly StatusEffectsSystem _statusEffects = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;
    private readonly ShaderInstance _rainbowShader;

    public float Intoxication = 0.0f;
    public float TimeTicker = 0.0f;
    public float Phase = 0.0f;

    private const float VisualThreshold = 10.0f;
    private const float PowerDivisor = 250.0f;
    private float _timeScale = 0.0f;
    private float _warpScale = 0.0f;

    private float EffectScale => Math.Clamp((Intoxication - VisualThreshold) / PowerDivisor, 0.0f, 1.0f);

    public RainbowOverlay()
    {
        IoCManager.InjectDependencies(this);

        _statusEffects = _sysMan.GetEntitySystem<StatusEffectsSystem>();

        _rainbowShader = _prototypeManager.Index(Shader).InstanceUnique();
        _config.OnValueChanged(CCVars.ReducedMotion, OnReducedMotionChanged, invokeImmediately: true);
    }

    private void OnReducedMotionChanged(bool reducedMotion)
    {
        _timeScale = reducedMotion ? 0.0f : 1.0f;
        _warpScale = reducedMotion ? 0.0f : 1.0f;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        var playerEntity = _playerManager.LocalEntity;

        if (playerEntity == null)
            return;

        if (!_statusEffects.TryGetEffectsEndTimeWithComp<SeeingRainbowsStatusEffectComponent>(playerEntity, out var endTime))
            return;

        endTime ??= TimeSpan.MaxValue;
        var timeLeft = (float)(endTime - _timing.CurTime).Value.TotalSeconds;

        TimeTicker += args.DeltaSeconds;
        if (timeLeft - TimeTicker > timeLeft / 16f)
        {
            Intoxication += (timeLeft - Intoxication) * args.DeltaSeconds / 16f;
        }
        else
        {
            Intoxication -= Intoxication / (timeLeft - TimeTicker) * args.DeltaSeconds;
        }
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_entityManager.TryGetComponent(_playerManager.LocalEntity, out EyeComponent? eyeComp))
            return false;

        if (args.Viewport.Eye != eyeComp.Eye)
            return false;

        return EffectScale > 0;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;
        _rainbowShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _rainbowShader.SetParameter("colorScale", EffectScale);
        _rainbowShader.SetParameter("timeScale", _timeScale);
        _rainbowShader.SetParameter("warpScale", _warpScale * EffectScale);
        _rainbowShader.SetParameter("phase", Phase);
        handle.UseShader(_rainbowShader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
