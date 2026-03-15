using Content.Server.Kitchen.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Kitchen.Components;
using Robust.Shared.Audio;
using Robust.Shared.Containers;

namespace Content.Server.Kitchen.EntitySystems;

public sealed partial class MicrowaveSystem
{
    /// <summary>
    ///     Adds ActivelyMicrowavedComponent to entities inserted into an active microwave.
    /// </summary>
    /// <param name="ent">The microwave entity.</param>
    private void OnActiveMicrowaveInsert(Entity<ActiveMicrowaveComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        var microwavedComp = AddComp<ActivelyMicrowavedComponent>(args.Entity);
        microwavedComp.Microwave = ent.Owner;
    }

    /// <summary>
    ///     Removes ActivelyMicrowavedComponent from entities removed from an active microwave.
    /// </summary>
    /// <param name="ent">The microwave entity.</param>
    private void OnActiveMicrowaveRemove(Entity<ActiveMicrowaveComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        RemCompDeferred<ActivelyMicrowavedComponent>(args.Entity);
    }

    /// <summary>
    ///     Updates the microwave UI when the microwave's solution changes.
    /// </summary>
    /// <param name="ent">The microwave entity.</param>
    // TODO: I'm not sure why this is here...?
    private void OnSolutionChange(Entity<MicrowaveComponent> ent, ref SolutionContainerChangedEvent args)
    {
        UpdateUserInterfaceState(ent);
    }

    /// <summary>
    ///     Updates the microwave UI when entities are added/removed from the microwave.
    /// </summary>
    /// <param name="uid">The microwave entity ID.</param>
    /// <param name="component">The microwave entity's component.</param>
    // For some reason ContainerModifiedMessage just can't be used at all with Entity<T>.
    // TODO: replace with Entity<T> syntax once that's possible
    private void OnContentUpdate(EntityUid uid, MicrowaveComponent component, ContainerModifiedMessage args)
    {
        if (component.Storage != args.Container)
            return;

        UpdateUserInterfaceState((uid, component));
    }

    /// <summary>
    ///     Check whether or not this microwave has space for the item, in both capacity and item size.
    /// </summary>
    /// <param name="ent">The microwave entity.</param>
    /// <param name="item">The item to attempt to insert.</param>
    /// <returns>Whether or not the item fits in the microwave. False if the item is too big or the microwave is full.</returns>
    private bool ItemFitsInMicrowave(Entity<MicrowaveComponent> ent, Entity<ItemComponent> item)
    {
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
            || HasComp<ActiveMicrowaveComponent>(ent.Owner)
            || !TryComp<ItemComponent>(args.EntityUid, out var item)
            || !ItemFitsInMicrowave(ent, (args.EntityUid, item)))
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
            _popupSystem.PopupEntity(Loc.GetString("microwave-component-interact-using-no-power"), ent, args.User);
            return;
        }

        // Microwave is broken
        if (ent.Comp.Broken)
        {
            _popupSystem.PopupEntity(Loc.GetString("microwave-component-interact-using-broken"), ent, args.User);
            return;
        }

        // Only items can be inserted into the microwave
        if (!TryComp<ItemComponent>(args.Used, out var item))
        {
            _popupSystem.PopupEntity(Loc.GetString("microwave-component-interact-using-transfer-fail"), ent, args.User);
            return;
        }

        // Item is too big for the microwave
        if (_item.GetSizePrototype(item.Size) > _item.GetSizePrototype(ent.Comp.MaxItemSize))
        {
            _popupSystem.PopupEntity(Loc.GetString("microwave-component-interact-item-too-big", ("item", args.Used)), ent, args.User);
            return;
        }

        // The microwave is full
        if (ent.Comp.Storage.Count >= ent.Comp.Capacity)
        {
            _popupSystem.PopupEntity(Loc.GetString("microwave-component-interact-full"), ent, args.User);
            return;
        }

        args.Handled = true;
        _handsSystem.TryDropIntoContainer(args.User, args.Used, ent.Comp.Storage);
        UpdateUserInterfaceState(ent);
    }

    /// <summary>
    ///     Ejects all ingredients from the microwave.
    /// </summary>
    /// <param name="ent">The microwave entity.</param>
    private void OnEjectMessage(Entity<MicrowaveComponent> ent, ref MicrowaveEjectMessage args)
    {
        if (!HasContents(ent) || HasComp<ActiveMicrowaveComponent>(ent))
            return;

        _container.EmptyContainer(ent.Comp.Storage);
        _audio.PlayPvs(ent.Comp.ClickSound, ent, AudioParams.Default.WithVolume(-2));
        UpdateUserInterfaceState(ent);
    }

    /// <summary>
    ///     Ejects an ingredient entity from the microwave.
    /// </summary>
    /// <param name="ent">The microwave entity.</param>
    private void OnEjectIndex(Entity<MicrowaveComponent> ent, ref MicrowaveEjectSolidIndexedMessage args)
    {
        if (!HasContents(ent) || HasComp<ActiveMicrowaveComponent>(ent))
            return;

        _container.Remove(GetEntity(args.EntityID), ent.Comp.Storage);
        UpdateUserInterfaceState(ent);
    }
}
