using Content.Shared.Actions;
using Content.Shared.Inventory.Events;
using Content.Shared.Ninja.Components;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Content.Shared.Timing;
using Robust.Shared.Audio;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Ninja.Systems;

public abstract class SharedNinjaSuitSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedNinjaGlovesSystem _gloves = default!;
    [Dependency] protected readonly SharedSpaceNinjaSystem _ninja = default!;
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

    /// <summary>
    /// Call the shared and serverside code for when a ninja equips the suit.
    /// </summary>
    private void OnEquipped(EntityUid uid, NinjaSuitComponent comp, GotEquippedEvent args)
    {
        var user = args.Equipee;
        if (!TryComp<SpaceNinjaComponent>(user, out var ninja))
            return;

        NinjaEquippedSuit(uid, comp, user, ninja);
    }

    /// <summary>
    /// Add all the actions when a suit is equipped.
    /// Since the event doesn't pass user this can't check if it's a ninja early and not add actions.
    /// </summary>
    private void OnGetItemActions(EntityUid uid, NinjaSuitComponent comp, GetItemActionsEvent args)
    {
        args.Actions.Add(comp.TogglePhaseCloakAction);
        args.Actions.Add(comp.RecallKatanaAction);
        args.Actions.Add(comp.CreateThrowingStarAction);
        args.Actions.Add(comp.KatanaDashAction);
        args.Actions.Add(comp.EmpAction);
    }

    /// <summary>
    /// Call the shared and serverside code for when anyone unequips a suit.
    /// </summary>
    private void OnUnequipped(EntityUid uid, NinjaSuitComponent comp, GotUnequippedEvent args)
    {
        UserUnequippedSuit(uid, comp, args.Equipee);
    }

    /// <summary>
    /// Called when a suit is equipped by a space ninja.
    /// In the future it might be changed to an explicit activation toggle/verb like gloves are.
    /// </summary>
    protected virtual void NinjaEquippedSuit(EntityUid uid, NinjaSuitComponent comp, EntityUid user, SpaceNinjaComponent ninja)
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
            Dirty(comp);
            SetCloaked(user, false);

            if (disableAbilities)
            {
                _audio.PlayPredicted(comp.RevealSound, uid, user);
                var useDelay = EnsureComp<UseDelayComponent>(user);
                useDelay.Delay = comp.DisableTime;
                _useDelay.BeginDelay(user, useDelay);
            }
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
        if (!TryComp<StealthComponent>(user, out var stealth))
            return;

        // prevent debug assert when ending round
        if (MetaData(user).EntityLifeStage >= EntityLifeStage.Terminating)
            return;

        // slightly visible, but doesn't change when moving so it's ok
        var visibility = cloaked ? stealth.MinVisibility + 0.3f : stealth.MaxVisibility;
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
        if (TryComp<SpaceNinjaComponent>(user, out var ninja))
        {
            _ninja.AssignSuit(ninja, null);
            // disable glove abilities
            if (ninja.Gloves != null && TryComp<NinjaGlovesComponent>(ninja.Gloves.Value, out var gloves))
                _gloves.DisableGloves(ninja.Gloves.Value, gloves);
        }

        // force uncloak the user
        comp.Cloaked = false;
        Dirty(comp);
        SetCloaked(user, false);
        RemComp<StealthComponent>(user);
    }

    /// <summary>
    /// Handle cloak setting message sent by the server.
    /// </summary>
    private void OnSetCloakedMessage(SetCloakedMessage msg)
    {
        if (TryComp<SpaceNinjaComponent>(msg.User, out var ninja) && TryComp<NinjaSuitComponent>(ninja.Suit, out var suit))
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
