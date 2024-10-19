using Content.Shared.Access.Systems;
using Content.Shared.Popups;
using Content.Shared._DeltaV.Shipyard.Prototypes;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._DeltaV.Shipyard;

/// <summary>
/// Handles shipyard console interaction.
/// <c>ShipyardSystem</c> does the heavy lifting serverside.
/// </summary>
public abstract class SharedShipyardConsoleSystem : EntitySystem
{
    [Dependency] protected readonly AccessReaderSystem _access = default!;
    [Dependency] protected readonly IPrototypeManager _proto = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<ShipyardConsoleComponent>(ShipyardConsoleUiKey.Key, subs =>
        {
            subs.Event<ShipyardConsolePurchaseMessage>(OnPurchase);
        });
    }

    private void OnPurchase(Entity<ShipyardConsoleComponent> ent, ref ShipyardConsolePurchaseMessage msg)
    {
        var user = msg.Actor;
        if (!_access.IsAllowed(user, ent.Owner))
        {
            Popup.PopupClient(Loc.GetString("comms-console-permission-denied"), ent, user);
            Audio.PlayPredicted(ent.Comp.DenySound, ent, user);
            return;
        }

        if (!_proto.TryIndex(msg.Vessel, out var vessel) || _whitelistSystem.IsWhitelistFail(vessel.Whitelist, ent))
            return;

        TryPurchase(ent, user, vessel);
    }

    protected virtual void TryPurchase(Entity<ShipyardConsoleComponent> ent, EntityUid user, VesselPrototype vessel)
    {
    }
}
