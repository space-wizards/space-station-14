using Content.Shared.Clothing.Components;
using Content.Shared.Ninja.Components;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Popups;
using Robust.Shared.GameStates;
using Robust.Shared.Network;

namespace Content.Shared.Ninja.Systems;

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
    public void AssignSuit(SpaceNinjaComponent comp, EntityUid? suit)
    {
        if (comp.Suit == suit)
            return;

        comp.Suit = suit;
        Dirty(comp);
    }

    /// <summary>
    /// Set the ninja's worn gloves entity
    /// </summary>
    public void AssignGloves(SpaceNinjaComponent comp, EntityUid? gloves)
    {
        if (comp.Gloves == gloves)
            return;

        comp.Gloves = gloves;
        Dirty(comp);
    }

    /// <summary>
    /// Bind a katana entity to a ninja, letting it be recalled and dash.
    /// </summary>
    public void BindKatana(SpaceNinjaComponent comp, EntityUid? katana)
    {
        if (comp.Katana == katana)
            return;

        comp.Katana = katana;
        Dirty(comp);
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
