using Content.Shared.Examine;
using Content.Shared.Inventory.Events;
using Content.Shared.Ninja.Components;
using Content.Shared.Popups;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Robust.Shared.Network;

namespace Content.Shared.Ninja.Systems;

public abstract class SharedNinjaSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] protected readonly SharedPopupSystem _popups = default!;
    [Dependency] protected readonly SharedStealthSystem _stealth = default!;

    public override void Initialize()
    {
    	base.Initialize();

        SubscribeLocalEvent<SpaceNinjaGlovesComponent, GotEquippedEvent>(OnGlovesEquipped);
        SubscribeLocalEvent<SpaceNinjaGlovesComponent, ExaminedEvent>(OnGlovesExamine);
        SubscribeLocalEvent<SpaceNinjaGlovesComponent, ToggleNinjaGlovesEvent>(OnToggleGloves);
        SubscribeLocalEvent<SpaceNinjaGlovesComponent, GotUnequippedEvent>(OnGlovesUnequipped);

        SubscribeLocalEvent<SpaceNinjaSuitComponent, GotEquippedEvent>(OnSuitEquipped);
        SubscribeLocalEvent<SpaceNinjaSuitComponent, GotUnequippedEvent>(OnSuitUnequipped);
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

    private void OnSuitEquipped(EntityUid uid, SpaceNinjaSuitComponent comp, GotEquippedEvent args)
    {
        var user = args.Equipee;
        if (!TryComp<SpaceNinjaComponent>(user, out var ninja))
            return;

        NinjaEquippedSuit(uid, comp, user, ninja);
    }

    private void OnSuitUnequipped(EntityUid uid, SpaceNinjaSuitComponent comp, GotUnequippedEvent args)
    {
        UserUnequippedSuit(uid, comp, args.Equipee);
	}

    /// <summary>
    /// Bind a katana entity to a ninja, letting it be recalled and dash.
    /// </summary>
    public void BindKatana(SpaceNinjaComponent comp, EntityUid katana)
    {
        comp.Katana = katana;
    }

	// TODO: remove when objective stuff moved into objectives somehow
    public void DetonateSpiderCharge(SpaceNinjaComponent comp)
    {
    	comp.SpiderChargeDetonated = true;
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

	/// <summary>
	/// Called when a suit is equipped by a space ninja.
	/// In the future it might be changed to an explicit activation toggle/verb like gloves are.
	/// </summary>
	protected virtual void NinjaEquippedSuit(EntityUid uid, SpaceNinjaSuitComponent comp, EntityUid user, SpaceNinjaComponent ninja)
	{
        // mark the user as wearing this suit, used when being attacked among other things
        ninja.Suit = uid;

        // initialize phase cloak
        AddComp<StealthComponent>(user);
        SetCloaked(user, comp.Cloaked);
    }

	/// <summary>
	/// Sets the stealth effect for a ninja cloaking.
	/// Does not update suit Cloaked field, has to be done yourself.
	/// </summary>
    protected void SetCloaked(EntityUid user, bool cloaked)
    {
        if (!TryComp<StealthComponent>(user, out var stealth))
            return;

        // slightly visible, but doesn't change when moving so it's ok
        var visibility = cloaked ? stealth.MinVisibility + 0.25f : stealth.MaxVisibility;
        _stealth.SetVisibility(user, visibility, stealth);
        _stealth.SetEnabled(user, cloaked, stealth);
    }

	/// <summary>
	/// Called when a suit is unequipped, not necessarily by a space ninja.
	/// In the future it might be changed to also have explicit deactivation via toggle.
	/// </summary>
	protected virtual void UserUnequippedSuit(EntityUid uid, SpaceNinjaSuitComponent comp, EntityUid user)
	{
        // mark the user as not wearing a suit
        if (TryComp<SpaceNinjaComponent>(user, out var ninja))
        {
            ninja.Suit = null;
            // disable glove abilities
            if (ninja.Gloves != null)
                DisableGloves(ninja.Gloves.Value, user);
        }

        // force uncloak the user
        comp.Cloaked = false;
        SetCloaked(user, false);
        RemComp<StealthComponent>(user);
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
