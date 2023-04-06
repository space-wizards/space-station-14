using Content.Shared.Actions;
using Content.Shared.Inventory.Events;
using Content.Shared.Ninja.Components;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Content.Shared.Timing;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Ninja.Systems;

public abstract class SharedNinjaSuitSystem : EntitySystem
{
    [Dependency] private readonly SharedNinjaGlovesSystem _gloves = default!;
    [Dependency] protected readonly SharedNinjaSystem _ninja = default!;
    [Dependency] private readonly SharedStealthSystem _stealth = default!;
    [Dependency] protected readonly UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NinjaSuitComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<NinjaSuitComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<NinjaSuitComponent, GotUnequippedEvent>(OnUnequipped);

        SubscribeNetworkEvent<SetCloakedMessage>(OnSetCloakedMessage);
    }

    private void OnEquipped(EntityUid uid, NinjaSuitComponent comp, GotEquippedEvent args)
    {
        var user = args.Equipee;
        if (!TryComp<NinjaComponent>(user, out var ninja))
            return;

        NinjaEquippedSuit(uid, comp, user, ninja);
    }

    private void OnGetItemActions(EntityUid uid, NinjaSuitComponent comp, GetItemActionsEvent args)
    {
        args.Actions.Add(comp.TogglePhaseCloakAction);
        args.Actions.Add(comp.RecallKatanaAction);
        // TODO: ninja stars instead of soap, when embedding is a thing
        // The cooldown should also be reduced from 10 to 1 or so
        args.Actions.Add(comp.CreateSoapAction);
        args.Actions.Add(comp.KatanaDashAction);
        args.Actions.Add(comp.EmpAction);
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
        EnsureComp<StealthComponent>(user);
        SetCloaked(user, comp.Cloaked);
    }

    /// <summary>
    /// Force uncloak the user, disables suit abilities if the bool is set.
    /// </summary>
    public void RevealNinja(EntityUid uid, NinjaSuitComponent comp, EntityUid user, bool disableAbilities = false)
    {
        if (comp.Cloaked)
        {
            comp.Cloaked = false;
            SetCloaked(user, false);
            // TODO: add the box open thing its funny

            if (disableAbilities)
                _useDelay.BeginDelay(uid);
        }
    }

    /// <summary>
    /// Returns the power used by a suit
    /// </summary>
    public float SuitWattage(NinjaSuitComponent suit)
    {
        float wattage = suit.PassiveWattage;
        if (suit.Cloaked)
            wattage += suit.CloakWattage;
        return wattage;
    }

    /// <summary>
    /// Sets the stealth effect for a ninja cloaking.
    /// Does not update suit Cloaked field, has to be done yourself.
    /// </summary>
    protected void SetCloaked(EntityUid user, bool cloaked)
    {
        if (!TryComp<StealthComponent>(user, out var stealth) || stealth.Deleted)
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

    private void OnSetCloakedMessage(SetCloakedMessage msg)
    {
        if (TryComp<NinjaComponent>(msg.User, out var ninja) && TryComp<NinjaSuitComponent>(ninja.Suit, out var suit))
        {
            suit.Cloaked = msg.Cloaked;
            SetCloaked(msg.User, msg.Cloaked);
        }
    }
}

/// <summary>
/// Calls SetCloaked on the client from the server, along with updating the suit Cloaked bool.
/// </summary>
[Serializable, NetSerializable]
public sealed class SetCloakedMessage : EntityEventArgs
{
    public EntityUid User;
    public bool Cloaked;
}
