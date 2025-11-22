
using System.Numerics;
using Content.Shared.Inventory;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Random;

namespace Content.Server.Weapons.Melee;

public sealed class ThrowEquippedOnHitSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ThrowingSystem _throw = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ThrowEquippedOnHitComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(Entity<ThrowEquippedOnHitComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.HitEntities.Count == 0)
            return;

        // Make sure your only targeting one entity
        var target = args.HitEntities[_random.Next(args.HitEntities.Count)];

        if (!_inventory.TryGetContainerSlotEnumerator(target, out var enumerator, ent.Comp.TargetSlots))
            return;

        while (enumerator.MoveNext(out var slot))
        {
            if (_random.NextDouble() >= ent.Comp.ThrowChance)
                continue;

            if (!_whitelist.CheckBoth(slot.ContainedEntity, ent.Comp.Blacklist, ent.Comp.Whitelist))
                continue;

            // TODO: Should this apply forensics from the glove -> punched item?

            if (!_inventory.TryUnequip(target, slot.ID, out var removedItem))
                continue;

            var direction = _transform.GetWorldPosition(target) - _transform.GetWorldPosition(args.User);

            // This comes up if you hit yourself.
            if (direction.EqualsApprox(Vector2.Zero))
                direction = _random.NextVector2();

            direction.Normalize();

            var throwAngle = _random.NextAngle(-ent.Comp.AngleVariance, ent.Comp.AngleVariance).RotateVec(direction);

            _throw.TryThrow(removedItem.Value, throwAngle);
        }
    }
}
