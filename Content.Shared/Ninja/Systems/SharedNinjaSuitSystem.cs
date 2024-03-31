using Content.Shared.Actions;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Inventory.Events;
using Content.Shared.Ninja.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Ninja.Systems;

/// <summary>
/// Handles (un)equipping and provides some API functions.
/// </summary>
public abstract class SharedNinjaSuitSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming GameTiming = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedNinjaGlovesSystem _gloves = default!;
    [Dependency] private readonly SharedSpaceNinjaSystem _ninja = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] protected readonly StealthClothingSystem StealthClothing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NinjaSuitComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<NinjaSuitComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<NinjaSuitComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<NinjaSuitComponent, AddStealthActionEvent>(OnAddStealthAction);
        SubscribeLocalEvent<NinjaSuitComponent, GotUnequippedEvent>(OnUnequipped);
    }

    private void OnMapInit(EntityUid uid, NinjaSuitComponent component, MapInitEvent args)
    {
        _actionContainer.EnsureAction(uid, ref component.RecallKatanaActionEntity, component.RecallKatanaAction);
        _actionContainer.EnsureAction(uid, ref component.CreateThrowingStarActionEntity, component.CreateThrowingStarAction);
        _actionContainer.EnsureAction(uid, ref component.EmpActionEntity, component.EmpAction);
        Dirty(uid, component);
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
    /// Add all the actions when a suit is equipped by a ninja.
    /// </summary>
    private void OnGetItemActions(EntityUid uid, NinjaSuitComponent comp, GetItemActionsEvent args)
    {
        if (!HasComp<SpaceNinjaComponent>(args.User))
            return;

        args.AddAction(ref comp.RecallKatanaActionEntity, comp.RecallKatanaAction);
        args.AddAction(ref comp.CreateThrowingStarActionEntity, comp.CreateThrowingStarAction);
        args.AddAction(ref comp.EmpActionEntity, comp.EmpAction);
    }

    /// <summary>
    /// Only add stealth clothing's toggle action when equipped by a ninja.
    /// </summary>
    private void OnAddStealthAction(EntityUid uid, NinjaSuitComponent comp, AddStealthActionEvent args)
    {
        if (!HasComp<SpaceNinjaComponent>(args.User))
            args.Cancel();
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
        _ninja.AssignSuit(user, uid, ninja);

        // initialize phase cloak, but keep it off
        StealthClothing.SetEnabled(uid, user, false);
    }

    /// <summary>
    /// Force uncloaks the user and disables suit abilities.
    /// </summary>
    public void RevealNinja(EntityUid uid, EntityUid user, bool disable = true, NinjaSuitComponent? comp = null, StealthClothingComponent? stealthClothing = null)
    {
        if (!Resolve(uid, ref comp, ref stealthClothing))
            return;

        if (!StealthClothing.SetEnabled(uid, user, false, stealthClothing))
            return;

        if (!disable)
            return;

        // previously cloaked, disable abilities for a short time
        _audio.PlayPredicted(comp.RevealSound, uid, user);
        Popup.PopupClient(Loc.GetString("ninja-revealed"), user, user, PopupType.MediumCaution);
        comp.DisableCooldown = GameTiming.CurTime + comp.DisableTime;
    }

    // TODO: modify PowerCellDrain
    /// <summary>
    /// Returns the power used by a suit
    /// </summary>
    public float SuitWattage(EntityUid uid, NinjaSuitComponent? suit = null)
    {
        if (!Resolve(uid, ref suit))
            return 0f;

        float wattage = suit.PassiveWattage;
        if (TryComp<StealthClothingComponent>(uid, out var stealthClothing) && stealthClothing.Enabled)
            wattage += suit.CloakWattage;
        return wattage;
    }

    /// <summary>
    /// Called when a suit is unequipped, not necessarily by a space ninja.
    /// In the future it might be changed to also have explicit deactivation via toggle.
    /// </summary>
    protected virtual void UserUnequippedSuit(EntityUid uid, NinjaSuitComponent comp, EntityUid user)
    {
        if (!TryComp<SpaceNinjaComponent>(user, out var ninja))
            return;

        // mark the user as not wearing a suit
        _ninja.AssignSuit(user, null, ninja);
        // disable glove abilities
        if (ninja.Gloves != null && TryComp<NinjaGlovesComponent>(ninja.Gloves.Value, out var gloves))
            _gloves.DisableGloves(ninja.Gloves.Value, gloves);
    }
}
