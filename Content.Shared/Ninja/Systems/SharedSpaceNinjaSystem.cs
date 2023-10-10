using Content.Shared.Clothing.Components;
using Content.Shared.Ninja.Components;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Popups;

namespace Content.Shared.Ninja.Systems;

/// <summary>
/// Provides shared ninja API, handles being attacked revealing ninja and stops guns from shooting.
/// </summary>
public abstract class SharedSpaceNinjaSystem : EntitySystem
{
    [Dependency] protected readonly SharedNinjaSuitSystem _suit = default!;
    [Dependency] protected readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpaceNinjaComponent, AttackedEvent>(OnNinjaAttacked);
        SubscribeLocalEvent<SpaceNinjaComponent, ShotAttemptedEvent>(OnShotAttempted);
    }

    /// <summary>
    /// Set the ninja's worn suit entity
    /// </summary>
    public void AssignSuit(EntityUid uid, EntityUid? suit, SpaceNinjaComponent? comp = null)
    {
        if (!Resolve(uid, ref comp) || comp.Suit == suit)
            return;

        comp.Suit = suit;
        Dirty(uid, comp);
    }

    /// <summary>
    /// Set the ninja's worn gloves entity
    /// </summary>
    public void AssignGloves(EntityUid uid, EntityUid? gloves, SpaceNinjaComponent? comp = null)
    {
        if (!Resolve(uid, ref comp) || comp.Gloves == gloves)
            return;

        comp.Gloves = gloves;
        Dirty(uid, comp);
    }

    /// <summary>
    /// Bind a katana entity to a ninja, letting it be recalled and dash.
    /// </summary>
    public void BindKatana(EntityUid uid, EntityUid? katana, SpaceNinjaComponent? comp = null)
    {
        if (!Resolve(uid, ref comp) || comp.Katana == katana)
            return;

        comp.Katana = katana;
        Dirty(uid, comp);
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
    private void OnNinjaAttacked(EntityUid uid, SpaceNinjaComponent comp, AttackedEvent args)
    {
        if (comp.Suit != null && TryComp<StealthClothingComponent>(comp.Suit, out var stealthClothing) && stealthClothing.Enabled)
        {
            _suit.RevealNinja(comp.Suit.Value, uid, null, stealthClothing);
        }
    }

    /// <summary>
    /// Require ninja to fight with HONOR, no guns!
    /// </summary>
    private void OnShotAttempted(EntityUid uid, SpaceNinjaComponent comp, ref ShotAttemptedEvent args)
    {
        _popup.PopupClient(Loc.GetString("gun-disabled"), uid, uid);
        args.Cancel();
    }
}
