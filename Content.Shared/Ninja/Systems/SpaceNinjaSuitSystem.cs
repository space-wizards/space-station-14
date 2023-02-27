using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Robust.Shared.Network;

namespace Content.Shared.Ninja.Systems;

public abstract class SharedSpaceNinjaSuitSystem : EntitySystem
{
    [Dependency] protected readonly SharedStealthSystem _stealth = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpaceNinjaSuitComponent, GotEquippedEvent>(OnSuitEquipped);
        SubscribeLocalEvent<SpaceNinjaSuitComponent, ComponentGetState>(OnSuitGetState);
        SubscribeLocalEvent<SpaceNinjaSuitComponent, ComponentHandleState>(OnSuitHandleState);
        SubscribeLocalEvent<SpaceNinjaSuitComponent, GotUnequippedEvent>(OnSuitUnequipped);
    }

    private void OnSuitEquipped(EntityUid uid, SpaceNinjaSuitComponent comp, GotEquippedEvent args)
    {
        var user = args.Equipee;
        if (!TryComp<SpaceNinjaComponent>(user, out var ninja))
            return;

        NinjaEquippedSuit(uid, comp, user, ninja);
    }

    private void OnSuitGetState(EntityUid uid, SpaceNinjaSuitComponent comp, ref ComponentGetState args)
    {
        args.State = new SpaceNinjaSuitComponentState(comp.Cloaked);
    }

    private void OnSuitHandleState(EntityUid uid, SpaceNinjaSuitComponent comp, ref ComponentHandleState args)
    {
        if (args.Current is not SpaceNinjaSuitComponentState state)
            return;

        comp.Cloaked = state.Cloaked;
    }

    private void OnSuitUnequipped(EntityUid uid, SpaceNinjaSuitComponent comp, GotUnequippedEvent args)
    {
        UserUnequippedSuit(uid, comp, args.Equipee);
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
}
