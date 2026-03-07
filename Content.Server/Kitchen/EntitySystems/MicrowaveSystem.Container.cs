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
    private void OnActiveMicrowaveInsert(Entity<ActiveMicrowaveComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        var microwavedComp = AddComp<ActivelyMicrowavedComponent>(args.Entity);
        microwavedComp.Microwave = ent.Owner;
    }

    private void OnActiveMicrowaveRemove(Entity<ActiveMicrowaveComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        RemCompDeferred<ActivelyMicrowavedComponent>(args.Entity);
    }

    private void OnSolutionChange(Entity<MicrowaveComponent> ent, ref SolutionContainerChangedEvent args)
    {
        UpdateUserInterfaceState(ent);
    }

    private void OnContentUpdate(EntityUid uid, MicrowaveComponent component, ContainerModifiedMessage args) // For some reason ContainerModifiedMessage just can't be used at all with Entity<T>. TODO: replace with Entity<T> syntax once that's possible
    {
        if (component.Storage != args.Container)
            return;

        UpdateUserInterfaceState(uid, component);
    }

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

    private void OnInteractUsing(Entity<MicrowaveComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!_power.IsPowered(ent.Owner))
        {
            _popupSystem.PopupEntity(Loc.GetString("microwave-component-interact-using-no-power"), ent, args.User);
            return;
        }

        if (ent.Comp.Broken)
        {
            _popupSystem.PopupEntity(Loc.GetString("microwave-component-interact-using-broken"), ent, args.User);
            return;
        }

        if (TryComp<ItemComponent>(args.Used, out var item))
        {
            // check if size of an item you're trying to put in is too big
            if (_item.GetSizePrototype(item.Size) > _item.GetSizePrototype(ent.Comp.MaxItemSize))
            {
                _popupSystem.PopupEntity(Loc.GetString("microwave-component-interact-item-too-big", ("item", args.Used)), ent, args.User);
                return;
            }
        }
        else
        {
            // check if thing you're trying to put in isn't an item
            _popupSystem.PopupEntity(Loc.GetString("microwave-component-interact-using-transfer-fail"), ent, args.User);
            return;
        }

        if (ent.Comp.Storage.Count >= ent.Comp.Capacity)
        {
            _popupSystem.PopupEntity(Loc.GetString("microwave-component-interact-full"), ent, args.User);
            return;
        }

        args.Handled = true;
        _handsSystem.TryDropIntoContainer(args.User, args.Used, ent.Comp.Storage);
        UpdateUserInterfaceState(ent, ent.Comp);
    }

    private void OnEjectMessage(Entity<MicrowaveComponent> ent, ref MicrowaveEjectMessage args)
    {
        if (!HasContents(ent) || HasComp<ActiveMicrowaveComponent>(ent))
            return;

        _container.EmptyContainer(ent.Comp.Storage);
        _audio.PlayPvs(ent.Comp.ClickSound, ent, AudioParams.Default.WithVolume(-2));
        UpdateUserInterfaceState(ent, ent.Comp);
    }

    private void OnEjectIndex(Entity<MicrowaveComponent> ent, ref MicrowaveEjectSolidIndexedMessage args)
    {
        if (!HasContents(ent) || HasComp<ActiveMicrowaveComponent>(ent))
            return;

        _container.Remove(GetEntity(args.EntityID), ent.Comp.Storage);
        UpdateUserInterfaceState(ent, ent.Comp);
    }
}
