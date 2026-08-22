using Content.Shared.Item;
using Content.Shared.Ninja.Components;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Popups;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Ninja.Events;
using Content.Shared.Projectiles;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Ninja.Systems;

/// <summary>
/// Provides shared ninja API, handles being attacked revealing ninja and stops guns from shooting.
/// </summary>
public abstract partial class SharedSpaceNinjaSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedActionsSystem _action = default!;
    [Dependency] private DisableActionSystem _disableAction = default!;
    [Dependency] protected SharedNinjaSuitSystem Suit = default!;
    [Dependency] protected SharedPopupSystem Popup = default!;

    [Dependency] public EntityQuery<SpaceNinjaComponent> NinjaQuery = default!;

    public bool IsNinja([NotNullWhen(true)] EntityUid? uid)
    {
        return NinjaQuery.HasComp(uid);
    }

    /// <summary>
    /// Set the ninja's worn suit entity
    /// </summary>
    public void AssignSuit(Entity<SpaceNinjaComponent> ent, EntityUid? suit)
    {
        if (ent.Comp.Suit == suit)
            return;

        ent.Comp.Suit = suit;
        Dirty(ent, ent.Comp);
    }

    /// <summary>
    /// Set the ninja's worn gloves entity
    /// </summary>
    public void AssignGloves(Entity<SpaceNinjaComponent> ent, EntityUid? gloves)
    {
        if (ent.Comp.Gloves == gloves)
            return;

        ent.Comp.Gloves = gloves;
        Dirty(ent, ent.Comp);
    }

    [SubscribeLocalEvent]
    private void OnBindItem(Entity<SpaceNinjaComponent> ent, ref BindItemEvent args)
    {
        if (ent.Comp.Katana != null)
            return;

        ent.Comp.Katana = args.Item;
        Dirty(ent);
    }

    /// <summary>
    /// Gets the user's battery and tries to use some charge from it, returning true if successful.
    /// Serverside only.
    /// </summary>
    public virtual bool TryUseCharge(EntityUid user, float charge)
    {
        return false;
    }

    [SubscribeLocalEvent]
    private void OnNinjaAttackedMelee(Entity<SpaceNinjaComponent> ninja, ref AttackedEvent args)
    {
        TryRevealNinja(ninja, disable: true);
    }

    /// <summary>
    /// Handle revealing ninja if cloaked when attacked.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnNinjaAttackedRanged(Entity<SpaceNinjaComponent> ninja, ref HitByProjectileEvent args)
    {
        TryRevealNinja(ninja, disable: true);
    }

    /// <summary>
    /// Handle revealing ninja if cloaked when attacked.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnNinjaAttackedRanged(Entity<SpaceNinjaComponent> ninja, ref HitByHitScanEvent args)
    {
        TryRevealNinja(ninja, disable: true);
    }

    /// <summary>
    /// Handle revealing ninja if cloaked when attacking.
    /// Only reveals, there is no cooldown.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnNinjaAttack(Entity<SpaceNinjaComponent> ent, ref MeleeAttackEvent args)
    {
        TryRevealNinja(ent, disable: false);
    }

    private void TryRevealNinja(Entity<SpaceNinjaComponent> ninja, bool disable)
    {
        if (ninja.Comp.Suit is not { } suit || !TryComp<NinjaSuitComponent>(ninja.Comp.Suit, out var suitComp))
            return;

        var revealed = Suit.RevealNinja((suit, suitComp), ninja);

        if (!revealed || !disable)
            return;

        var ev = new NinjaAbilitiesDisabledEvent();
        RaiseLocalEvent(ninja, ref ev);

        // previously cloaked, disable abilities for a short time
        _audio.PlayPredicted(suitComp.RevealSound, ninja, ninja);
        Popup.PopupEntity(Loc.GetString("ninja-revealed"), ninja, ninja, PopupType.MediumCaution);
    }

    /// <summary>
    /// This disables the sword's dash for the disable duration.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnAbilitiesDisabled(Entity<DisableActionComponent> ability, ref ActionRelayedEvent<NinjaAbilitiesDisabledEvent> args)
    {
        _disableAction.DisableAction(ability.AsNullable());
    }

    /// <summary>
    /// Require ninja to fight with HONOR, no guns!
    /// </summary>
    [SubscribeLocalEvent]
    private void OnShotAttempted(Entity<SpaceNinjaComponent> ent, ref ShotAttemptedEvent args)
    {
        Popup.PopupEntity(Loc.GetString("gun-disabled"), ent, ent);
        args.Cancel();
    }
}
