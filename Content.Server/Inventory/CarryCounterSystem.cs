using System;
using System.Collections.Generic;
using Content.Shared.Stacks;
using Robust.Shared.Containers;

namespace Content.Server.Inventory;

/// Read-only helper to count what a mob is carrying, recursively,
/// with support for stackable items (cash, sheets, etc.).
/// "Carrying" = anything in the mob's containers (inventory slots, hands,
/// backpack, boxes, wallets, nested containers, implants, etc.).
public sealed class CarryCounterSystem : EntitySystem
{
    private EntityQuery<ContainerManagerComponent> _containerQuery;

    public override void Initialize()
    {
        base.Initialize();
        _containerQuery = GetEntityQuery<ContainerManagerComponent>();
    }

    /// Overload that accepts a nullable EntityUid (e.g., Mind.OwnedEntity).
    /// Returns 0 if null; otherwise forwards to the non-nullable overload.
    public int CountByStackType(EntityUid? maybeMob, string stackTypeId, int valuePerUnit = 1)
    {
        if (!maybeMob.HasValue)
            return 0;

        return CountByStackType(maybeMob.Value, stackTypeId, valuePerUnit);
    }

    /// Returns the total amount of items carried by mob whose
    /// stack type (if present) matches <paramref name="stackTypeId"/>.
    /// If an entity has a StackComponent with a matching StackTypeId,
    /// contributes stack.Count * valuePerUnit. Non-stack entities contribute 0 in this overload.
    public int CountByStackType(EntityUid mob, string stackTypeId, int valuePerUnit = 1)
    {
        if (!_containerQuery.TryGetComponent(mob, out var currentManager))
            return 0;

        valuePerUnit = Math.Max(1, valuePerUnit);

        var total = 0;
        var managerStack = new Stack<ContainerManagerComponent>();

        // Walk all containers owned by the mob (inventory, hands, implants, etc.)
        do
        {
            foreach (var container in currentManager.Containers.Values)
            {
                foreach (var entity in container.ContainedEntities)
                {
                    total += CountEntityByStackType(entity, stackTypeId, valuePerUnit);

                    // Recurse into nested containers (bags, boxes, wallets, etc.)
                    if (_containerQuery.TryGetComponent(entity, out var childManager))
                        managerStack.Push(childManager);
                }
            }
        } while (managerStack.TryPop(out currentManager));

        return total;
    }

    /// Generic counter
    /// For stacks, contributes Count by default; for non-stacks contributes 1.
    public int CountWhere(
        EntityUid mob,
        Func<EntityUid, bool> predicate,
        Func<EntityUid, int>? valueSelector = null)
    {
        if (!_containerQuery.TryGetComponent(mob, out var currentManager))
            return 0;

        valueSelector ??= DefaultValueSelector;

        var total = 0;
        var managerStack = new Stack<ContainerManagerComponent>();

        do
        {
            foreach (var container in currentManager.Containers.Values)
            {
                foreach (var entity in container.ContainedEntities)
                {
                    if (predicate(entity))
                        total += Math.Max(0, valueSelector(entity));

                    if (_containerQuery.TryGetComponent(entity, out var childManager))
                        managerStack.Push(childManager);
                }
            }
        } while (managerStack.TryPop(out currentManager));

        return total;

        int DefaultValueSelector(EntityUid ent)
        {
            if (TryComp(ent, out StackComponent? s))
                return Math.Max(0, s.Count);
            return 1;
        }
    }

    // --- helpers ---

    private int CountEntityByStackType(EntityUid entity, string stackTypeId, int valuePerUnit)
    {
        var sum = 0;

        if (TryComp(entity, out StackComponent? stack) && stack.StackTypeId == stackTypeId)
            sum += Math.Max(0, stack.Count) * valuePerUnit;

        // Also check any nested containers owned by this entity.
        if (_containerQuery.TryGetComponent(entity, out var nested))
        {
            foreach (var container in nested.Containers.Values)
            {
                foreach (var child in container.ContainedEntities)
                    sum += CountEntityByStackType(child, stackTypeId, valuePerUnit);
            }
        }

        return sum;
    }
}
