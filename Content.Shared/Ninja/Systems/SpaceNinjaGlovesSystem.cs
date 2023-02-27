using Content.Shared.Examine;
using Content.Shared.Inventory.Events;
using Content.Shared.Ninja.Components;
using Content.Shared.Popups;
using Robust.Shared.Network;

namespace Content.Shared.Ninja.Systems;

public class SpaceNinjaGlovesSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] protected readonly SharedPopupSystem _popups = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpaceNinjaGlovesComponent, GotEquippedEvent>(OnGlovesEquipped);
        SubscribeLocalEvent<SpaceNinjaGlovesComponent, ExaminedEvent>(OnGlovesExamine);
        SubscribeLocalEvent<SpaceNinjaGlovesComponent, ToggleNinjaGlovesEvent>(OnToggleGloves);
        SubscribeLocalEvent<SpaceNinjaGlovesComponent, GotUnequippedEvent>(OnGlovesUnequipped);
    }

    private void OnGlovesEquipped(EntityUid uid, SpaceNinjaGlovesComponent comp, GotEquippedEvent args)
    {
        var user = args.Equipee;
        if (TryComp<SpaceNinjaComponent>(user, out var ninja))
            NinjaEquippedGloves(uid, comp, user, ninja);
    }

    private void OnGlovesExamine(EntityUid uid, SpaceNinjaGlovesComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var enabled = HasComp<GlovesEnabledComponent>(uid);
        args.PushText(Loc.GetString(enabled ? "ninja-gloves-examine-on" : "ninja-gloves-examine-off"));
    }

    private void OnToggleGloves(EntityUid uid, SpaceNinjaGlovesComponent comp, ToggleNinjaGlovesEvent args)
    {
        var user = args.Performer;
        // need to wear suit to enable gloves
        if (!TryComp<SpaceNinjaComponent>(user, out var ninja)
            || ninja.Suit == null
            || !HasComp<SpaceNinjaSuitComponent>(ninja.Suit.Value))
        {
            ClientPopup(Loc.GetString("ninja-gloves-not-wearing-suit"), user);
            return;
        }

        var enabled = !HasComp<GlovesEnabledComponent>(uid);
        if (enabled)
            AddComp<GlovesEnabledComponent>(uid);
        else
            RemComp<GlovesEnabledComponent>(uid);

        var message = Loc.GetString(enabled ? "ninja-gloves-on" : "ninja-gloves-off");
        ClientPopup(message, user);
    }

    private void OnGlovesUnequipped(EntityUid uid, SpaceNinjaGlovesComponent comp, GotUnequippedEvent args)
    {
        var user = args.Equipee;
        UserUnequippedGloves(uid, comp, user);
    }

    /// <summary>
    /// Called when gloves are equipped by a Space Ninja.
    /// </summary>
    protected virtual void NinjaEquippedGloves(EntityUid uid, SpaceNinjaGlovesComponent comp, EntityUid user, SpaceNinjaComponent ninja)
    {
        ninja.Gloves = uid;
    }

    /// <summary>
    /// Called when gloves are unequipped by anyone.
    /// </summary>
    protected virtual void UserUnequippedGloves(EntityUid uid, SpaceNinjaGlovesComponent comp, EntityUid user)
    {
        DisableGloves(uid, user);
        if (TryComp<SpaceNinjaComponent>(user, out var ninja))
            ninja.Gloves = null;
    }

    // popups used in shared will be duplicated if sent by server
    private void ClientPopup(string msg, EntityUid user)
    {
        if (_net.IsClient)
            _popups.PopupEntity(msg, user, user);
    }

    private void DisableGloves(EntityUid uid, EntityUid user)
    {
        if (HasComp<GlovesEnabledComponent>(uid))
        {
            RemComp<GlovesEnabledComponent>(uid);
            ClientPopup(Loc.GetString("ninja-gloves-off"), user);
        }
    }
}
