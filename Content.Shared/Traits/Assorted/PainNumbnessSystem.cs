using Content.Shared.Mobs.Systems;

namespace Content.Shared.Traits.Assorted;

public sealed class PainNumbnessSystem : EntitySystem
{
    [Dependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PainNumbnessComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<PainNumbnessComponent, ComponentRemove>(OnComponentRemove);

    }

    private void OnComponentRemove(EntityUid uid, PainNumbnessComponent component, ComponentRemove args)
    {
        var ev = new DamageOverlayUpdateEvent(uid);
        RaiseLocalEvent(uid, ev, true);
        _mobThresholdSystem.VerifyThresholds(uid);
    }

    private void OnComponentInit(EntityUid uid, PainNumbnessComponent component, ComponentInit args)
    {
        var ev = new DamageOverlayUpdateEvent(uid);
        RaiseLocalEvent(uid, ev, true);
        _mobThresholdSystem.VerifyThresholds(uid);
    }
}

public record struct DamageOverlayUpdateEvent(EntityUid Target);
