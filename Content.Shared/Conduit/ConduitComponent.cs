using Content.Shared.Conduit.Holder;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Conduit;

/// <summary>
/// Attached to entities that act as a conduit for the transportation of other entities
/// (e.g., disposal pipes and transit tubes). Conduits have a defined set of directions
/// in which transported entities can move while within it.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedConduitSystem))]
public sealed partial class ConduitComponent : Component
{
    /// <summary>
    /// Sound played when entities passing through the conduit when they change direction.
    /// </summary>
    [DataField]
    public SoundSpecifier ClangSound = new SoundPathSpecifier("/Audio/Effects/clang.ogg", AudioParams.Default.WithVolume(-5f));

    /// <summary>
    /// Damage dealt to entities passing through the conduit when they change direction.
    /// </summary>
    [DataField]
    public DamageSpecifier DamageOnTurn = new()
    {
        DamageDict = new()
        {
            { "Blunt", 0.0 },
        }
    };

    /// <summary>
    /// Array of directions that entities can potentially exit the conduit.
    /// </summary>
    /// <remarks>
    /// The direction that entities will exit preferentially follows the order the list (from most to least).
    /// A direction will be skipped if it is opposite to the direction the entity entered,
    /// or if the angular difference between the entry and potential exit is less than <see cref="MaxDeltaAngle"/>.
    /// </remarks>
    [DataField]
    public Direction[] Exits = { Direction.South };

    /// <summary>
    /// The largest angle that entities can turn while traveling through the conduit.
    /// This only applies when there are more than two potential exits.
    /// </summary>
    [DataField]
    public Angle MaxDeltaAngle = 180;

    /// <summary>
    /// Sets the type of conduit. Only conduits of the same type connect to each other.
    /// </summary>
    [DataField]
    public ConduitType ConduitType = ConduitType.Disposals;
}

/// <summary>
/// Event raised when determining which direction a conduit holder should head next.
/// </summary>
/// <param name="Holder">The conduit holder.</param>
[ByRefEvent]
public record struct GetConduitNextDirectionEvent(Entity<ConduitHolderComponent> Holder)
{
    public Direction Next;
}

/// <summary>
/// The type of conduit.
/// </summary>
/// <remarks>Only conduits of same type connect to each other.</remarks>
[Serializable, NetSerializable]
public enum ConduitType
{
    Disposals,
    Transit
}
