using Content.Shared.Ninja.Components;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Popups;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Ninja.Systems;

/// <summary>
/// Provides shared ninja API, handles being attacked revealing ninja and stops guns from shooting.
/// </summary>
public abstract class SharedSpaceNinjaSystem : EntitySystem
{
    [Dependency] protected readonly SharedNinjaSuitSystem Suit = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;

    public EntityQuery<SpaceNinjaComponent> NinjaQuery;

    public override void Initialize()
    {
        base.Initialize();

        NinjaQuery = GetEntityQuery<SpaceNinjaComponent>();

        SubscribeLocalEvent<SpaceNinjaComponent, AttackedEvent>(OnNinjaAttacked);
        SubscribeLocalEvent<SpaceNinjaComponent, MeleeAttackEvent>(OnNinjaAttack);
        SubscribeLocalEvent<SpaceNinjaComponent, ShotAttemptedEvent>(OnShotAttempted);
    }

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

    /// <summary>
    /// Bind a katana entity to a ninja, letting it be recalled and dash.
    /// Does nothing if the player is not a ninja or already has a katana bound.
    /// </summary>
    public void BindKatana(Entity<SpaceNinjaComponent?> ent, EntityUid katana)
    {
        if (!NinjaQuery.Resolve(ent, ref ent.Comp, false) || ent.Comp.Katana != null)
            return;

        ent.Comp.Katana = katana;
        Dirty(ent, ent.Comp);
    }

    /// <summary>
    /// Gets the user's battery and tries to use some charge from it, returning true if successful.
    /// Serverside only.
    /// </summary>
    public virtual bool TryUseCharge(EntityUid user, float charge)
    {
        return false;
    }

    /// <summary>
    /// Handle revealing ninja if cloaked when attacked.
    /// </summary>
    private void OnNinjaAttacked(Entity<SpaceNinjaComponent> ent, ref AttackedEvent args)
    {
        TryRevealNinja(ent, disable: true);
    }

    /// <summary>
    /// Handle revealing ninja if cloaked when attacking.
    /// Only reveals, there is no cooldown.
    /// </summary>
    private void OnNinjaAttack(Entity<SpaceNinjaComponent> ent, ref MeleeAttackEvent args)
    {
        TryRevealNinja(ent, disable: false);
    }

    private void TryRevealNinja(Entity<SpaceNinjaComponent> ent, bool disable)
    {
        if (ent.Comp.Suit is {} uid && TryComp<NinjaSuitComponent>(ent.Comp.Suit, out var suit))
            Suit.RevealNinja((uid, suit), ent, disable: disable);
    }

    /// <summary>
    /// Require ninja to fight with HONOR, no guns!
    /// </summary>
    private void OnShotAttempted(Entity<SpaceNinjaComponent> ent, ref ShotAttemptedEvent args)
    {
        Popup.PopupClient(Loc.GetString("gun-disabled"), ent, ent);
        args.Cancel();
    }
}
