using Content.Shared.Construction.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Kitchen.Components;
using Robust.Shared.Containers;

namespace Content.Shared.Kitchen.EntitySystems;

public abstract partial class SharedMicrowaveSystem
{
    /// <summary>
    ///     Initialize container-related events for microwaves.
    /// </summary>
    private void InitializeContainer()
    {
        SubscribeLocalEvent<MicrowaveComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<MicrowaveComponent, ContainerIsInsertingAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<MicrowaveComponent, EntInsertedIntoContainerMessage>(OnContentsUpdated);
        SubscribeLocalEvent<MicrowaveComponent, EntRemovedFromContainerMessage>(OnContentsUpdated);
        SubscribeLocalEvent<MicrowaveComponent, InteractUsingEvent>(OnInteractUsing,
            after: [typeof(AnchorableSystem)]);
    }

    /// <summary>
    ///     Initializes the microwave's storage container.
    /// </summary>
    /// <param name="ent">The microwave entity.</param>
    private void OnComponentInit(Entity<MicrowaveComponent> ent, ref ComponentInit args)
    {
        // this really does have to be in ComponentInit
        ent.Comp.Storage = ContainerSys.EnsureContainer<Container>(ent, ent.Comp.ContainerId);
    }

    /// <summary>
    ///     Check whether or not this microwave has space for the item, in both capacity and item size.
    /// </summary>
    /// <param name="ent">The microwave entity.</param>
    /// <param name="item">The item to attempt to insert.</param>
    /// <returns>Whether or not the item fits in the microwave. False if the item is too big or the microwave is full.</returns>
    private bool CanFitInMicrowave(Entity<MicrowaveComponent?> ent, Entity<ItemComponent?> item)
    {
        if (!Resolve(ent.Owner, ref ent.Comp) || !Resolve(item.Owner, ref item.Comp))
            return false;

        var microwave = ent.Comp;
        if (microwave.Storage.Count >= microwave.Capacity)
            return false;

        var maxSize = _item.GetSizePrototype(microwave.MaxItemSize);
        var itemSize = _item.GetSizePrototype(item.Comp.Size);
        if (itemSize > maxSize)
            return false;

        return true;
    }

    /// <summary>
    ///     Prevents inserting entities into the microwave if the microwave is broken, active,
    ///     or the item is invalid.
    /// </summary>
    /// <param name="ent">The microwave entity.</param>
    private void OnInsertAttempt(Entity<MicrowaveComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (args.Container.ID != ent.Comp.ContainerId)
            return;

        if (ent.Comp.Broken
            || IsActiveMicrowave(ent.AsNullable())
            || !CanFitInMicrowave(ent.AsNullable(), args.EntityUid))
        {
            args.Cancel();
            return;
        }
    }

    /// <summary>
    ///     Attempt to insert an entity into the microwave, resulting in a pop-up message if this is not possible.
    /// </summary>
    /// <param name="ent">The microwave entity.</param>
    private void OnInteractUsing(Entity<MicrowaveComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        // Power is turned off
        if (!_power.IsPowered(ent.Owner))
        {
            var message = Loc.GetString("microwave-component-interact-using-no-power");
            PopupSys.PopupPredicted(message, ent, args.User);
            return;
        }

        // Microwave is broken
        if (ent.Comp.Broken)
        {
            var message = Loc.GetString("microwave-component-interact-using-broken");
            PopupSys.PopupPredicted(message, ent, args.User);
            return;
        }

        // Only items can be inserted into the microwave
        if (!TryComp<ItemComponent>(args.Used, out var item))
        {
            var message = Loc.GetString("microwave-component-interact-using-transfer-fail");
            PopupSys.PopupPredicted(message, ent, args.User);
            return;
        }

        // Item is too big for the microwave
        if (_item.GetSizePrototype(item.Size) > _item.GetSizePrototype(ent.Comp.MaxItemSize))
        {
            var message = Loc.GetString("microwave-component-interact-item-too-big", ("item", args.Used));
            PopupSys.PopupPredicted(message, ent, args.User);
            return;
        }

        // The microwave is full
        if (ent.Comp.Storage.Count >= ent.Comp.Capacity)
        {
            var message = Loc.GetString("microwave-component-interact-full");
            PopupSys.PopupPredicted(message, ent, args.User);
            return;
        }

        _hands.TryDropIntoContainer(args.User, args.Used, ent.Comp.Storage);
        args.Handled = true;
    }

    /// <summary>
    ///     Updates the microwave UI when entities are added/removed from the microwave.
    /// </summary>
    /// <param name="uid">The microwave entity ID.</param>
    /// <param name="component">The microwave entity's component.</param>
    // For some reason ContainerModifiedMessage just can't be used at all with Entity<T>.
    // TODO: replace with Entity<T> syntax once that's possible
    private void OnContentsUpdated(EntityUid uid, MicrowaveComponent component, ContainerModifiedMessage args)
    {
        if (component.Storage != args.Container)
            return;

        UpdateUserInterfaceState((uid, component));
    }
}
