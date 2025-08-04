using Content.Shared.Throwing;
using Content.Shared.Trigger.Components.Triggers;

namespace Content.Shared.Trigger.Systems;

public sealed partial class TriggerOnlandSystem : EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnLandComponent, LandEvent>(OnLand);
    }

    private void OnLand(Entity<TriggerOnLandComponent> ent, ref LandEvent args)
    {
        _trigger.Trigger(ent.Owner, args.User, ent.Comp.KeyOut);
    }
}
