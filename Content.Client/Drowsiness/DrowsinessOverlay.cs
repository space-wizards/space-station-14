using Content.Shared.Bed.Sleep;
using Content.Shared.Drowsiness;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Drowsiness;

public sealed class DrowsinessOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntitySystemManager _sysMan = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;
    private readonly ShaderInstance _drowsinessShader;

    public float CurrentPower = 0.0f;

    private const float PowerDivisor = 250.0f;
    private const float Intensity = 0.2f; // for adjusting the visual scale
    private float _visualScale = 0; // between 0 and 1

    public DrowsinessOverlay()
    {
        IoCManager.InjectDependencies(this);
        _drowsinessShader = _prototypeManager.Index<ShaderPrototype>("Drowsiness").InstanceUnique();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        var playerEntity = _playerManager.LocalEntity;

        if (playerEntity == null)
            return;

        var statusSys = _sysMan.GetEntitySystem<SharedStatusEffectsSystem>();

        if (!statusSys.TryEffectWithComp<DrowsinessStatusEffectComponent>(playerEntity, out var drowsinessEffects))
            return;

        TimeSpan? remainingTime = TimeSpan.Zero;
        foreach (var effect in drowsinessEffects)
        {
            if (!_entityManager.TryGetComponent<StatusEffectComponent>(effect, out var statusEffect))
                continue;

            if (statusEffect.EndEffectTime > remainingTime)
                remainingTime = statusEffect.EndEffectTime;
        }

        if (remainingTime is null)
            return;

        var curTime = _timing.CurTime;
        var timeLeft = (float)(remainingTime - curTime).Value.TotalSeconds;

        CurrentPower += 8f * (0.5f * timeLeft - CurrentPower) * args.DeltaSeconds / (timeLeft + 1);
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_entityManager.TryGetComponent(_playerManager.LocalEntity, out EyeComponent? eyeComp))
            return false;

        if (args.Viewport.Eye != eyeComp.Eye)
            return false;

        _visualScale = Math.Clamp(CurrentPower / PowerDivisor, 0.0f, 1.0f);
        return _visualScale > 0;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;
        _drowsinessShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _drowsinessShader.SetParameter("Strength", _visualScale * Intensity);
        handle.UseShader(_drowsinessShader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
