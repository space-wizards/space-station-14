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
    /// Sets the station grid entity that the ninja was spawned near.
    /// </summary>
    public void SetStationGrid(SpaceNinjaComponent comp, EntityUid? grid)
    {
        comp.StationGrid = grid;
    }

    /// <summary>
    /// Set the ninja's worn suit entity
    /// </summary>
    public void AssignSuit(SpaceNinjaComponent comp, EntityUid? suit)
    {
        comp.Suit = suit;
    }

    /// <summary>
    /// Set the ninja's worn gloves entity
    /// </summary>
    public void AssignGloves(SpaceNinjaComponent comp, EntityUid? gloves)
    {
        comp.Gloves = gloves;
    }

    /// <summary>
    /// Bind a katana entity to a ninja, letting it be recalled and dash.
    /// </summary>
    public void BindKatana(SpaceNinjaComponent comp, EntityUid? katana)
    {
        comp.Katana = katana;
    }

    /// <summary>
    /// Drain power from a target battery into the ninja's suit battery, returning whether it succeeded.
    /// Serverside only, client assumes success.
    /// </summary>
    public virtual bool TryDrainPower(EntityUid user, NinjaDrainComponent drain, EntityUid target)
    {
        return true;
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
    /// Increment number of doors jacked for greentext.
    /// Only done on server duh
    /// </summary>
    public virtual void Doorjacked(EntityUid user)
    {
    }

    /// <summary>
    /// Handle revealing ninja if cloaked when attacked.
    /// </summary>
    private void OnNinjaAttacked(EntityUid uid, SpaceNinjaComponent comp, AttackedEvent args)
    {
        if (comp.Suit != null && TryComp<NinjaSuitComponent>(comp.Suit, out var suit) && suit.Cloaked)
        {
            _suit.RevealNinja(comp.Suit.Value, suit, uid, true);
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
