using Content.Shared.Mobs.Components;
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
        if (!HasComp<MobThresholdsComponent>(uid))
            return;

        _mobThresholdSystem.VerifyThresholds(uid);
    }

    private void OnComponentInit(EntityUid uid, PainNumbnessComponent component, ComponentInit args)
    {
        if (!HasComp<MobThresholdsComponent>(uid))
            return;

        _mobThresholdSystem.VerifyThresholds(uid);
    }
}
