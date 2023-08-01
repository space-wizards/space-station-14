using Content.Server.Cuffs;
using Content.Server.Store.Components;
using Content.Server.Store.Systems;
using Content.Shared.Cuffs.Components;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Server.Polymorph.Systems;

namespace Content.Server.Implants;

public sealed class SubdermalImplantSystem : SharedSubdermalImplantSystem
{
    [Dependency] private readonly CuffableSystem _cuffable = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SubdermalImplantComponent, UseFreedomImplantEvent>(OnFreedomImplant);
        SubscribeLocalEvent<StoreComponent, ImplantRelayEvent<AfterInteractUsingEvent>>(OnStoreRelay);
        SubscribeLocalEvent<SubdermalImplantComponent, ActivateImplantEvent>(OnActivateImplantEvent);
        SubscribeLocalEvent<SubdermalImplantComponent, UseDnaScramblerImplantEvent>(OnDnaScramblerImplant);

    }

    private void OnStoreRelay(EntityUid uid, StoreComponent store, ImplantRelayEvent<AfterInteractUsingEvent> implantRelay)
    {
        var args = implantRelay.Event;

        if (args.Handled)
            return;

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

    private void OnFreedomImplant(EntityUid uid, SubdermalImplantComponent component, UseFreedomImplantEvent args)
    {
        if (!TryComp<CuffableComponent>(component.ImplantedEntity, out var cuffs) || cuffs.Container.ContainedEntities.Count < 1)
            return;

        _cuffable.Uncuff(component.ImplantedEntity.Value, cuffs.LastAddedCuffs, cuffs.LastAddedCuffs);
        args.Handled = true;
    }

    private void OnActivateImplantEvent(EntityUid uid, SubdermalImplantComponent component, ActivateImplantEvent args)
    {
        args.Handled = true;
    }

    private void OnDnaScramblerImplant(EntityUid uid, SubdermalImplantComponent component, UseDnaScramblerImplantEvent args)
    {
        if (component.ImplantedEntity == null)
            return;

        var newIdentity = _polymorph.PolymorphEntity(component.ImplantedEntity.Value, "Scrambled");

        //checks if someone is trying to use a dna scrambler implant while already scrambled
        if (newIdentity == null)
        {
            _popup.PopupEntity(Loc.GetString("scramble-attempt-while-scrambled-popup"), component.ImplantedEntity.Value, component.ImplantedEntity.Value);
            return;
        }

        _popup.PopupEntity(Loc.GetString("scramble-implant-activated-popup", ("identity", newIdentity.Value)), newIdentity.Value, newIdentity.Value);

        args.Handled = true;
        QueueDel(uid);
    }
}
