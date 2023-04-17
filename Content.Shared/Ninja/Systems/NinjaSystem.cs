using Content.Shared.Ninja.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.GameStates;
using Robust.Shared.Network;

namespace Content.Shared.Ninja.Systems;

public abstract class SharedNinjaSystem : EntitySystem
{
    [Dependency] protected readonly SharedNinjaSuitSystem _suit = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NinjaComponent, AttackedEvent>(OnNinjaAttacked);
    }

    /// <summary>
    /// Sets the station grid entity that the ninja was spawned near.
    /// </summary>
    public void SetStationGrid(NinjaComponent comp, EntityUid? grid)
    {
        comp.StationGrid = grid;
    }

    /// <summary>
    /// Set the ninja's worn suit entity
    /// </summary>
    public void AssignSuit(NinjaComponent comp, EntityUid? suit)
    {
        comp.Suit = suit;
    }

    /// <summary>
    /// Set the ninja's worn gloves entity
    /// </summary>
    public void AssignGloves(NinjaComponent comp, EntityUid? gloves)
    {
        comp.Gloves = gloves;
    }

    /// <summary>
    /// Bind a katana entity to a ninja, letting it be recalled and dash.
    /// </summary>
    public void BindKatana(NinjaComponent comp, EntityUid? katana)
    {
        comp.Katana = katana;
    }

    /// <summary>
    /// Drain power from a target battery into the ninja's suit battery.
    /// Serverside only.
    /// </summary>
    public virtual void TryDrainPower(EntityUid user, NinjaDrainComponent drain, EntityUid target)
    {
    }

    /// <summary>
    /// Gets the user's battery and tries to use some charge from it, returning true if successful.
    /// Serverside only.
    /// </summary>
    public virtual bool TryUseCharge(EntityUid user, float charge)
    {
        return false;
    }

    private void OnNinjaAttacked(EntityUid uid, NinjaComponent comp, AttackedEvent args)
    {
        if (comp.Suit != null && TryComp<NinjaSuitComponent>(comp.Suit, out var suit) && suit.Cloaked)
        {
            _suit.RevealNinja(comp.Suit.Value, suit, uid, true);
        }
    }
}
