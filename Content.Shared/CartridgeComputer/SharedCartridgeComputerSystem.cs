using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Containers;

namespace Content.Shared.CartridgeComputer;

public abstract class SharedCartridgeComputerSystem : EntitySystem
{
    [Dependency] protected readonly ItemSlotsSystem ItemSlotsSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharedCartridgeComputerComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<SharedCartridgeComputerComponent, ComponentRemove>(OnComponentRemove);

        SubscribeLocalEvent<SharedCartridgeComputerComponent, EntInsertedIntoContainerMessage>(OnItemInserted);
        SubscribeLocalEvent<SharedCartridgeComputerComponent, EntRemovedFromContainerMessage>(OnItemRemoved);
    }

    protected void UpdateAppearanceData(EntityUid uid, SharedCartridgeComputerComponent computer)
    {
        _appearanceSystem.SetData(uid, CartridgeComputerVisuals.CartridgeInserted, computer.CartridgeSlot.HasItem);
    }

    private void OnComponentInit(EntityUid uid, SharedCartridgeComputerComponent computer, ComponentInit args)
    {
        ItemSlotsSystem.AddItemSlot(uid, SharedCartridgeComputerComponent.CartridgeSlotId, computer.CartridgeSlot);
    }

    private void OnComponentRemove(EntityUid uid, SharedCartridgeComputerComponent computer, ComponentRemove args)
    {
        ItemSlotsSystem.RemoveItemSlot(uid, computer.CartridgeSlot);
    }

    private void OnItemInserted(EntityUid uid, SharedCartridgeComputerComponent computer, EntInsertedIntoContainerMessage args)
    {
        RaiseLocalEvent(args.Entity, new CartridgeAddedEvent(uid));
        UpdateAppearanceData(uid, computer);
    }

    protected virtual void OnItemRemoved(EntityUid uid, SharedCartridgeComputerComponent computer, EntRemovedFromContainerMessage args)
    {
        RaiseLocalEvent(args.Entity, new CartridgeRemovedEvent(uid));
        UpdateAppearanceData(uid, computer);
    }
}

public sealed class CartridgeAddedEvent : EntityEventArgs
{
    public readonly EntityUid Computer;

    public CartridgeAddedEvent(EntityUid computer)
    {
        Computer = computer;
    }
}

public sealed class CartridgeRemovedEvent : EntityEventArgs
{
    public readonly EntityUid Computer;

    public CartridgeRemovedEvent(EntityUid computer)
    {
        Computer = computer;
    }
}

public sealed class CartridgeActivatedEvent : EntityEventArgs
{
    public readonly EntityUid Computer;

    public CartridgeActivatedEvent(EntityUid computer)
    {
        Computer = computer;
    }
}

public sealed class CartridgeDeactivatedEvent : EntityEventArgs
{
    public readonly EntityUid Computer;

    public CartridgeDeactivatedEvent(EntityUid computer)
    {
        Computer = computer;
    }
}
