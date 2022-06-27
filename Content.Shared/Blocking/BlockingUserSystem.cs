using Content.Shared.Damage;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;

namespace Content.Shared.Blocking;

public sealed class BlockingUserSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlockingUserComponent, DamageChangedEvent>(OnDamgageChanged);
    }

    private void OnDamgageChanged(EntityUid uid, BlockingUserComponent component, DamageChangedEvent args)
    {
        var items = _handsSystem.EnumerateHeld(uid);

        foreach (var item in items)
        {
            if (HasComp<BlockingComponent>(item))
                RaiseLocalEvent(item, args);
        }
    }
}
