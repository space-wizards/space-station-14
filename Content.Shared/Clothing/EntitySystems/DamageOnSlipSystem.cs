using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Slippery;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Content.Shared.Damage;

namespace Content.Shared.Clothing.EntitySystems;

public sealed class BreakOnSlipSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageOnSlipComponent, InventoryRelayedEvent<SlippedEvent>>(OnSlip);
    }

    private void OnSlip(Entity<DamageOnSlipComponent> ent, ref InventoryRelayedEvent<SlippedEvent> args)
    {
        if (!_random.Prob(ent.Comp.DamageChance) || _net.IsClient)
            return;
        if (ent.Comp.MultiplierMax is null)
        {
            _damageableSystem.TryChangeDamage(ent.Owner, ent.Comp.Damage);
        }
        else
        {
            var damage = ent.Comp.Damage * _random.NextFloat(1, ent.Comp.MultiplierMax.Value);
            _damageableSystem.TryChangeDamage(ent.Owner, damage);
        }

    }
}
