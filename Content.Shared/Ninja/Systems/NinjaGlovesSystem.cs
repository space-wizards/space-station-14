using Content.Shared.Examine;
using Content.Shared.Inventory.Events;
using Content.Shared.Ninja.Components;
using Content.Shared.Popups;

namespace Content.Shared.Ninja.Systems;

public abstract class SharedNinjaGlovesSystem : EntitySystem
{
    [Dependency] private readonly SharedNinjaSystem _ninja = default!;
    [Dependency] protected readonly SharedPopupSystem _popups = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NinjaGlovesComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<NinjaGlovesComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<NinjaGlovesComponent, ToggleNinjaGlovesEvent>(OnToggle);
        SubscribeLocalEvent<NinjaGlovesComponent, GotUnequippedEvent>(OnUnequipped);
    }

    /// <summary>
    /// Disable glove abilities and show the popup if they were enabled previously.
    /// </summary>
    public void DisableGloves(NinjaGlovesComponent comp, EntityUid user)
    {
        if (comp.Enabled)
        {
            comp.Enabled = false;
            _popups.PopupEntity(Loc.GetString("ninja-gloves-off"), user, user);
        }
    }

    private void OnEquipped(EntityUid uid, NinjaGlovesComponent comp, GotEquippedEvent args)
    {
        var user = args.Equipee;
        if (TryComp<NinjaComponent>(user, out var ninja))
            NinjaEquippedGloves(uid, comp, user, ninja);
    }

    private void OnExamine(EntityUid uid, NinjaGlovesComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushText(Loc.GetString(comp.Enabled ? "ninja-gloves-examine-on" : "ninja-gloves-examine-off"));
    }

    private void OnToggle(EntityUid uid, NinjaGlovesComponent comp, ToggleNinjaGlovesEvent args)
    {
        var user = args.Performer;
        // need to wear suit to enable gloves
        if (!TryComp<NinjaComponent>(user, out var ninja)
            || ninja.Suit == null
            || !HasComp<NinjaSuitComponent>(ninja.Suit.Value))
        {
            _popups.PopupEntity(Loc.GetString("ninja-gloves-not-wearing-suit"), user, user);
            return;
        }

        comp.Enabled = !comp.Enabled;
        var message = Loc.GetString(comp.Enabled ? "ninja-gloves-on" : "ninja-gloves-off");
        _popups.PopupEntity(message, user, user);
    }

    private void OnUnequipped(EntityUid uid, NinjaGlovesComponent comp, GotUnequippedEvent args)
    {
        var user = args.Equipee;
        UserUnequippedGloves(uid, comp, user);
    }

    /// <summary>
    /// Called when gloves are equipped by a Space Ninja.
    /// </summary>
    protected virtual void NinjaEquippedGloves(EntityUid uid, NinjaGlovesComponent comp, EntityUid user, NinjaComponent ninja)
    {
        _ninja.AssignGloves(ninja, uid);
    }

    /// <summary>
    /// Called when gloves are unequipped by anyone.
    /// </summary>
    protected virtual void UserUnequippedGloves(EntityUid uid, NinjaGlovesComponent comp, EntityUid user)
    {
        DisableGloves(comp, user);
        if (TryComp<NinjaComponent>(user, out var ninja))
            _ninja.AssignGloves(ninja, null);
    }
}
