using Content.Shared.Trigger.Components.Triggers;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared.Trigger.Systems;
public sealed partial class TriggerOnGunEmptySystem : EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnGunEmptyComponent, OnEmptyGunShotEvent>(OnEmptyGunShot);
    }

    private void OnEmptyGunShot(Entity<TriggerOnGunEmptyComponent> ent, ref OnEmptyGunShotEvent args)
    {
        _trigger.Trigger(ent.Owner, args.User, ent.Comp.KeyOut);
    }
}
