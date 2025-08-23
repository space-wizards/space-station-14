using Content.Server.Store.Components;
using Content.Server.Store.Systems;
using Content.Shared.Implants;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Store.Components;
using Robust.Shared.Collections;
using Robust.Shared.Map.Components;
using Content.Server.Polymorph.Systems; // Starlight
using Content.Shared.Zombies; // Starlight
using Robust.Shared.Player;
using Content.Shared.Implants.Components; // Starlight

namespace Content.Server.Implants;

public sealed class SubdermalImplantSystem : SharedSubdermalImplantSystem
{
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoreComponent, ImplantRelayEvent<AfterInteractUsingEvent>>(OnStoreRelay);
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
        if (!_store.TryAddCurrency((args.Used, currency), (uid, store)))
            return;

        args.Handled = true;
        var msg = Loc.GetString("store-currency-inserted-implant", ("used", args.Used));
        _popup.PopupEntity(msg, args.User, args.User);
    }

    #region Starlight
    /// </summary>
    /// <param name="uid">The entity to get implants from</param>
    /// <param name="implants">The list of implants found</param>
    /// <returns>True if the entity has implants, false otherwise</returns>
    public bool TryGetImplants(EntityUid uid, out List<EntityUid> implants)
    {
        implants = new List<EntityUid>();

        if (!TryComp<ImplantedComponent>(uid, out var implanted))
            return false;

        var implantContainer = implanted.ImplantContainer;

        if (implantContainer.ContainedEntities.Count == 0)
            return false;

        implants.AddRange(implantContainer.ContainedEntities);
        return true;
    }
    #endregion
}
