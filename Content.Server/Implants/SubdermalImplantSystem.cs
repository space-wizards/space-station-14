using Content.Server.Cuffs;
using Content.Server.Store.Components;
using Content.Server.Store.Systems;
using Content.Shared.Cuffs.Components;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Robust.Shared.Containers;

namespace Content.Server.Implants;

public sealed class SubdermalImplantSystem : SharedSubdermalImplantSystem
{
    [Dependency] private readonly CuffableSystem _cuffable = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StoreSystem _store = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SubdermalImplantComponent, UseFreedomImplantEvent>(OnFreedomImplant);

        SubscribeLocalEvent<StoreComponent, AfterInteractUsingEvent>(OnUplinkInteractUsing);

        SubscribeLocalEvent<ImplantedComponent, MobStateChangedEvent>(RelayToImplantEvent);
        SubscribeLocalEvent<ImplantedComponent, AfterInteractUsingEvent>(RelayToImplantEvent);
        SubscribeLocalEvent<ImplantedComponent, SuicideEvent>(RelayToImplantEvent);
    }

    private void OnFreedomImplant(EntityUid uid, SubdermalImplantComponent component, UseFreedomImplantEvent args)
    {
        if (!TryComp<CuffableComponent>(component.ImplantedEntity, out var cuffs) || cuffs.Container.ContainedEntities.Count < 1)
            return;

        _cuffable.Uncuff(component.ImplantedEntity.Value, cuffs.LastAddedCuffs, cuffs.LastAddedCuffs);
        args.Handled = true;
    }

    private void OnUplinkInteractUsing(EntityUid uid, StoreComponent store, AfterInteractUsingEvent args)
    {
        // can only insert into yourself to prevent uplink checking with renault
        if (args.Target != args.User)
            return;

        if (!TryComp<CurrencyComponent>(args.Used, out var currency))
            return;

        // same as store code, but message is only shown to yourself
        args.Handled = _store.TryAddCurrency(_store.GetCurrencyValue(args.Used, currency), uid, store);

        if (!args.Handled)
            return;

        var msg = Loc.GetString("store-currency-inserted-implant", ("used", args.Used));
        _popup.PopupEntity(msg, args.User, args.User);
        QueueDel(args.Used);
    }

    #region Relays

    //Relays from the implanted to the implant
    private void RelayToImplantEvent<T>(EntityUid uid, ImplantedComponent component, T args) where T: notnull
    {
        if (!_container.TryGetContainer(uid, ImplanterComponent.ImplantSlotId, out var implantContainer))
            return;
        foreach (var implant in implantContainer.ContainedEntities)
        {
            RaiseLocalEvent(implant, args);
        }
    }

    //Relays from the implanted to the implant
    private void RelayToImplantEventByRef<T>(EntityUid uid, ImplantedComponent component, ref T args) where T: notnull
    {
        if (!_container.TryGetContainer(uid, ImplanterComponent.ImplantSlotId, out var implantContainer))
            return;
        foreach (var implant in implantContainer.ContainedEntities)
        {
            RaiseLocalEvent(implant,ref args);
        }
    }

    //Relays from the implant to the implanted
    private void RelayToImplantedEvent<T>(EntityUid uid, SubdermalImplantComponent component, T args) where T : EntityEventArgs
    {
        if (component.ImplantedEntity != null)
        {
            RaiseLocalEvent(component.ImplantedEntity.Value, args);
        }
    }

    private void RelayToImplantedEventByRef<T>(EntityUid uid, SubdermalImplantComponent component, ref T args) where T : EntityEventArgs
    {
        if (component.ImplantedEntity != null)
        {
            RaiseLocalEvent(component.ImplantedEntity.Value, ref args);
        }
    }

    #endregion
}
