using Content.Shared.Sticky;
using Content.Shared.Trigger.Components.Triggers;

namespace Content.Shared.Trigger.Systems;

public sealed class TriggerOnStuckSystem : EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnStuckComponent, EntityStuckEvent>(OnStuck);
    }

    private void OnStuck(Entity<TriggerOnStuckComponent> ent, ref EntityStuckEvent args)
    {
        _trigger.Trigger(ent.Owner, args.User, ent.Comp.KeyOut);
    }
}
