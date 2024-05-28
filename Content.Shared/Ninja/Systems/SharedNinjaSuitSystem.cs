using Content.Shared.Actions;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Inventory.Events;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Ninja.Components;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Ninja.Systems;

/// <summary>
/// Handles (un)equipping and provides some API functions.
/// </summary>
public abstract class SharedNinjaSuitSystem : EntitySystem
{
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] private readonly SharedSpaceNinjaSystem _ninja = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NinjaSuitComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<NinjaSuitComponent, ClothingGotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<NinjaSuitComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<NinjaSuitComponent, ToggleClothingCheckEvent>(OnCloakCheck);
        SubscribeLocalEvent<NinjaSuitComponent, CheckItemCreatorEvent>(OnStarCheck);
        SubscribeLocalEvent<NinjaSuitComponent, CreateItemAttemptEvent>(OnCreateStarAttempt);
        SubscribeLocalEvent<NinjaSuitComponent, ItemToggleActivateAttemptEvent>(OnActivateAttempt);
        SubscribeLocalEvent<NinjaSuitComponent, GotUnequippedEvent>(OnUnequipped);
    }

    private void OnEquipped(Entity<NinjaSuitComponent> ent, ref ClothingGotEquippedEvent args)
    {
        var user = args.Wearer;
        if (_ninja.NinjaQuery.TryComp(user, out var ninja))
            NinjaEquipped(ent, (user, ninja));
    }

    protected virtual void NinjaEquipped(Entity<NinjaSuitComponent> ent, Entity<SpaceNinjaComponent> user)
    {
        // mark the user as wearing this suit, used when being attacked among other things
        _ninja.AssignSuit(user, ent);
    }

    private void OnMapInit(Entity<NinjaSuitComponent> ent, ref MapInitEvent args)
    {
        var (uid, comp) = ent;
        _actionContainer.EnsureAction(uid, ref comp.RecallKatanaActionEntity, comp.RecallKatanaAction);
        _actionContainer.EnsureAction(uid, ref comp.EmpActionEntity, comp.EmpAction);
        Dirty(uid, comp);
    }

    /// <summary>
    /// Add all the actions when a suit is equipped by a ninja.
    /// </summary>
    private void OnGetItemActions(Entity<NinjaSuitComponent> ent, ref GetItemActionsEvent args)
    {
        if (!_ninja.IsNinja(args.User))
            return;

        var comp = ent.Comp;
        args.AddAction(ref comp.RecallKatanaActionEntity, comp.RecallKatanaAction);
        args.AddAction(ref comp.EmpActionEntity, comp.EmpAction);
    }

    /// <summary>
    /// Only add toggle cloak action when equipped by a ninja.
    /// </summary>
    private void OnCloakCheck(Entity<NinjaSuitComponent> ent, ref ToggleClothingCheckEvent args)
    {
        if (!_ninja.IsNinja(args.User))
            args.Cancelled = true;
    }

    private void OnStarCheck(Entity<NinjaSuitComponent> ent, ref CheckItemCreatorEvent args)
    {
        if (!_ninja.IsNinja(args.User))
            args.Cancelled = true;
    }

    private void OnCreateStarAttempt(Entity<NinjaSuitComponent> ent, ref CreateItemAttemptEvent args)
    {
        if (CheckDisabled(ent, args.User))
            args.Cancelled = true;
    }

    /// <summary>
    /// Call the shared and serverside code for when anyone unequips a suit.
    /// </summary>
    private void OnUnequipped(Entity<NinjaSuitComponent> ent, ref GotUnequippedEvent args)
    {
        var user = args.Equipee;
        if (_ninja.NinjaQuery.TryComp(user, out var ninja))
            UserUnequippedSuit(ent, (user, ninja));
    }

    /// <summary>
    /// Force uncloaks the user and disables suit abilities.
    /// </summary>
    public void RevealNinja(Entity<NinjaSuitComponent?> ent, EntityUid user, bool disable = true)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var uid = ent.Owner;
        var comp = ent.Comp;
        if (_toggle.TryDeactivate(uid, user) || !disable)
            return;

        // previously cloaked, disable abilities for a short time
        _audio.PlayPredicted(comp.RevealSound, uid, user);
        Popup.PopupClient(Loc.GetString("ninja-revealed"), user, user, PopupType.MediumCaution);
        _useDelay.TryResetDelay(uid, id: comp.DisableDelayId);
    }

    private void OnActivateAttempt(Entity<NinjaSuitComponent> ent, ref ItemToggleActivateAttemptEvent args)
    {
        if (!_ninja.IsNinja(args.User))
        {
            args.Cancelled = true;
            return;
        }

        if (IsDisabled((ent, ent.Comp, null)))
        {
            args.Cancelled = true;
            args.Popup = Loc.GetString("ninja-suit-cooldown");
        }
    }

    /// <summary>
    /// Returns true if the suit is currently disabled
    /// </summary>
    public bool IsDisabled(Entity<NinjaSuitComponent?, UseDelayComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2))
            return false;

        return _useDelay.IsDelayed((ent, ent.Comp2), ent.Comp1.DisableDelayId);
    }

    protected bool CheckDisabled(Entity<NinjaSuitComponent> ent, EntityUid user)
    {
        if (IsDisabled((ent, ent.Comp, null)))
        {
            Popup.PopupEntity(Loc.GetString("ninja-suit-cooldown"), user, user, PopupType.Medium);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Called when a suit is unequipped, not necessarily by a space ninja.
    /// In the future it might be changed to also have explicit deactivation via toggle.
    /// </summary>
    protected virtual void UserUnequippedSuit(Entity<NinjaSuitComponent> ent, Entity<SpaceNinjaComponent> user)
    {
        // mark the user as not wearing a suit
        _ninja.AssignSuit(user, null);
        // disable glove abilities
        if (user.Comp.Gloves is {} uid)
            _toggle.TryDeactivate(uid, user: user);
    }
}
