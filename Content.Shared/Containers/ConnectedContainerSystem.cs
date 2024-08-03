using System.Diagnostics.CodeAnalysis;
using Content.Shared.Chemistry.Components;
using Content.Shared.Inventory;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;

namespace Content.Shared.Containers;

public class ConnectedContainerSystem : EntitySystem
{
    [Dependency] protected readonly SharedContainerSystem _containers = default!;
    [Dependency] protected readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] protected readonly InventorySystem _inventory = default!;


    public bool TryGetConnectedContainer(EntityUid subject, [NotNullWhen(true)] out EntityUid? slotEntity)
    {
        if (!TryComp<ConnectedContainerComponent>(subject, out var component))
        {
            slotEntity = null;
            return false;
        }

        return TryGetConnectedContainer(subject, component.TargetSlot, component.ContainerWhitelist, out slotEntity);
    }

    private bool TryGetConnectedContainer(EntityUid subject, SlotFlags slotFlag, EntityWhitelist? providerWhitelist, [NotNullWhen(true)] out EntityUid? slotEntity)
    {
        slotEntity = null;

        if (!_containers.TryGetContainingContainer((subject, Transform(subject)), out var container))
            return false;

        var user = container.Owner;
        if (!_inventory.TryGetContainerSlotEnumerator(user, out var enumerator, slotFlag))
            return false;

        while (enumerator.NextItem(out var item))
        {
            if (_whitelistSystem.IsWhitelistFailOrNull(providerWhitelist, item))
                continue;

            slotEntity = item;
            return true;
        }

        return false;
    }
}
