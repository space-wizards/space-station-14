using Content.Server.Destructible;
using Content.Shared.Crayon;
using Content.Shared.Trigger.Systems;

namespace Content.Server.Crayon;

public sealed partial class FakeConsumableSystem : SharedFakeConsumableSystem
{
    [Dependency] private TriggerSystem _trigger = default!;

    [SubscribeLocalEvent]
    private void OnDamageThresholdReached(Entity<FakeConsumableComponent> ent, ref DamageThresholdReached args)
    {
        var contained = RevealItem(ent, null);
        if (!contained.HasValue)
            return;

        var item = contained.Value;
        RaiseLocalEvent(item, args, true);
        _trigger.Trigger(item);
    }
}
