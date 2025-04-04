using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Inventory.ShitRockable.Components;
using Content.Shared.Damage;
using Content.Shared.Random;
using Robust.Shared.Random;
using Content.Shared.Throwing;
using Content.Shared.Administration.Logs;
using Robust.Shared.Network;

namespace Content.Shared.Inventory.ShitRockable;

/// <summary>
/// Handles messing with an entity's inventories when it gets its shit rocked.
/// </summary>
public sealed class ShitRockableSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly RandomHelperSystem _randomHelperSys = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminlogs = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShitRockableComponent, ShitRockedEvent>(OnShitRocked);
    }

    private void OnShitRocked(EntityUid uid, ShitRockableComponent component, ShitRockedEvent args)
    {
        var xformQuery = GetEntityQuery<TransformComponent>();

        if (args.RecievedDamage == null)
            return;

        if (component.SlotThresholds == null)
            return;

        var slotEnumerator = _inventory.GetSlotEnumerator(uid);
        while (slotEnumerator.NextItem(out var rocked, out var slotdef))
        {
            if (!TryComp<RockableItemComponent>(rocked, out var rockedComp))
                continue;

            foreach (var slotDamageThresh in component.SlotThresholds)
            {
                // relate our slot defs and slot flags
                var slots = slotDamageThresh.Slots;
                if (slots == SlotFlags.NONE || slotdef.SlotFlags != slots)
                    continue;

                // have we recieved enough of the right damage?
                var damageThresh = slotDamageThresh.DamageThreshold;
                if (damageThresh.GEQ(args.RecievedDamage))
                    continue;

                // some probability of getting your shit rocked. Maybe one day we get targetting, but this aint that.
                // check on the item itself via RockableItemComp
                if (_random.Prob(rockedComp.Chance) && _net.IsServer)
                {
                    // breaks glasses etc.
                    _damageable.TryChangeDamage(rocked, args.RecievedDamage);

                    _adminlogs.Add(Database.LogType.Damaged, Database.LogImpact.Low, $"The {ToPrettyString(rocked)} was knocked out of {ToPrettyString(uid)}'s inventory.");

                    // remove item to floor
                    if (!rockedComp.DontThrow && _inventory.TryUnequip(uid, slotdef.Name, silent: true))
                    {
                        _throwing.TryThrow(
                            rocked,
                            xformQuery.GetComponent(rocked).Coordinates.Offset(_random.NextVector2(2))
                            );
                    }

                }

            }

        }

    }

}
