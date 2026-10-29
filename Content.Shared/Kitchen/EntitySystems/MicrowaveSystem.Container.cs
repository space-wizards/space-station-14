using Content.Shared.Construction.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Kitchen.Components;
using Robust.Shared.Containers;

namespace Content.Shared.Kitchen.EntitySystems;

public abstract partial class MicrowaveSystem
{
    /// <summary>
    /// Initializes the microwave's storage container.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnComponentInit(Entity<MicrowaveComponent> ent, ref ComponentInit args)
    {
        // this really does have to be in ComponentInit
        ent.Comp.Storage = Container.EnsureContainer<Container>(ent, ent.Comp.ContainerId);
    }

    /// <summary>
    /// Prevents inserting entities into the microwave if the microwave is broken, active,
    /// or the item is invalid.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnInsertAttempt(Entity<MicrowaveComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (Timing.ApplyingState)
            return;

        if (args.Container.ID != ent.Comp.ContainerId)
            return;

        if (ent.Comp.Broken
            || IsActiveMicrowave(ent.AsNullable())
            || !CanFitInMicrowave(ent.AsNullable(), args.EntityUid))
        {
            args.Cancel();
        }
    }

    /// <summary>
    /// Attempt to insert an entity into the microwave, resulting in a pop-up message if this is not possible.
    /// </summary>
    [SubscribeLocalEvent(after:[typeof(AnchorableSystem)])]
    private void OnInteractUsing(Entity<MicrowaveComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        // Power is turned off
        if (!_power.IsPowered(ent.Owner))
        {
            var message = Loc.GetString("microwave-component-interact-using-no-power");
            Popup.PopupEntity(message, ent, args.User);
            return;
        }

        // Microwave is broken
        if (ent.Comp.Broken)
        {
            var message = Loc.GetString("microwave-component-interact-using-broken");
            Popup.PopupEntity(message, ent, args.User);
            return;
        }

        // Only items can be inserted into the microwave
        if (!ItemQuery.TryComp(args.Used, out var item))
        {
            var message = Loc.GetString("microwave-component-interact-using-transfer-fail");
            Popup.PopupEntity(message, ent, args.User);
            return;
        }

        // Item is too big for the microwave
        if (_item.GetSizePrototype(item.Size) > _item.GetSizePrototype(ent.Comp.MaxItemSize))
        {
            var message = Loc.GetString("microwave-component-interact-item-too-big", ("item", args.Used));
            Popup.PopupEntity(message, ent, args.User);
            return;
        }

        // The microwave is full
        if (ent.Comp.Storage.Count >= ent.Comp.Capacity)
        {
            var message = Loc.GetString("microwave-component-interact-full");
            Popup.PopupEntity(message, ent, args.User);
            return;
        }

        _hands.TryDropIntoContainer(args.User, args.Used, ent.Comp.Storage);
        args.Handled = true;
    }
    [SubscribeLocalEvent]
    private void OnContentsAdded(Entity<MicrowaveComponent> entity, ref EntInsertedIntoContainerMessage args)
        => OnContentsUpdated(entity, ref args);

    [SubscribeLocalEvent]
    private void OnContentsRemoved(Entity<MicrowaveComponent> entity, ref EntRemovedFromContainerMessage args)
        => OnContentsUpdated(entity, ref args);

    /// <summary>
    /// Updates the microwave UI when entities are added/removed from the microwave.
    /// </summary>
    // For some reason ContainerModifiedMessage just can't be used at all with Entity<T>.
    private void OnContentsUpdated<T>(Entity<MicrowaveComponent> entity, ref T args) where T : ContainerModifiedMessage
    {
        if (entity.Comp.Storage != args.Container)
            return;

        UpdateUI(entity.AsNullable());
    }

    /// <summary>
    /// Check whether or not this microwave has space for the item, in both capacity and item size.
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
}
