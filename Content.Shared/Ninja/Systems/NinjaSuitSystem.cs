using Content.Shared.Inventory.Events;
using Content.Shared.Ninja.Components;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Network;

namespace Content.Shared.Ninja.Systems;

public abstract class SharedNinjaSuitSystem : EntitySystem
{
    [Dependency] protected readonly SharedNinjaGlovesSystem _gloves = default!;
    [Dependency] protected readonly SharedNinjaSystem _ninja = default!;
    [Dependency] protected readonly SharedStealthSystem _stealth = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NinjaSuitComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<NinjaSuitComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<NinjaSuitComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<NinjaSuitComponent, GotUnequippedEvent>(OnUnequipped);
    }

    private void OnEquipped(EntityUid uid, NinjaSuitComponent comp, GotEquippedEvent args)
    {
        var user = args.Equipee;
        if (!TryComp<NinjaComponent>(user, out var ninja))
            return;

        NinjaEquippedSuit(uid, comp, user, ninja);
    }

    private void OnGetState(EntityUid uid, NinjaSuitComponent comp, ref ComponentGetState args)
    {
        args.State = new NinjaSuitComponentState(comp.Cloaked);
    }

    private void OnHandleState(EntityUid uid, NinjaSuitComponent comp, ref ComponentHandleState args)
    {
        if (args.Current is not NinjaSuitComponentState state)
            return;

        comp.Cloaked = state.Cloaked;
    }

    private void OnUnequipped(EntityUid uid, NinjaSuitComponent comp, GotUnequippedEvent args)
    {
        UserUnequippedSuit(uid, comp, args.Equipee);
    }

    /// <summary>
    /// Called when a suit is equipped by a space ninja.
    /// In the future it might be changed to an explicit activation toggle/verb like gloves are.
    /// </summary>
    protected virtual void NinjaEquippedSuit(EntityUid uid, NinjaSuitComponent comp, EntityUid user, NinjaComponent ninja)
    {
        // mark the user as wearing this suit, used when being attacked among other things
        _ninja.AssignSuit(ninja, uid);

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
    protected virtual void UserUnequippedSuit(EntityUid uid, NinjaSuitComponent comp, EntityUid user)
    {
        // mark the user as not wearing a suit
        if (TryComp<NinjaComponent>(user, out var ninja))
        {
            _ninja.AssignSuit(ninja, null);
            // disable glove abilities
            if (ninja.Gloves != null && TryComp<NinjaGlovesComponent>(ninja.Gloves.Value, out var gloves))
                _gloves.DisableGloves(gloves, user);
        }

        // force uncloak the user
        comp.Cloaked = false;
        SetCloaked(user, false);
        RemComp<StealthComponent>(user);
    }
}
