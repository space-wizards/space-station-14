using Content.Shared.Damage;
using Content.Shared.Disposal.Unit;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Disposal.Tube;

/// <summary>
/// Basic component required by all disposal pipes.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedDisposalTubeSystem))]
public sealed partial class DisposalTubeComponent : Component
{
    /// <summary>
    /// Sound played when entities passing through this pipe change direction.
    /// </summary>
    [DataField]
    public SoundSpecifier ClangSound = new SoundPathSpecifier("/Audio/Effects/clang.ogg", AudioParams.Default.WithVolume(-5f));

    /// <summary>
    /// Container of entities that are currently inside this tube.
    /// </summary>
    [DataField]
    public DisposalHolderComponent? Contents;

    /// <summary>
    /// Damage dealt to containing entities on every turn.
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
    /// Array of directions that entities can potentially exit the disposal tube from.
    /// </summary>
    /// <remarks>
    /// The direction that entities will exit preferentially follows the order the list (from most to least).
    /// A direction will be skipped if it is the opposite to the direction the entity entered,
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
    /// Determines the type of disposal pipe -
    /// only pipes of the same type can connect to each other.
    /// </summary>
    [DataField]
    public DisposalTubeType DisposalTubeType = DisposalTubeType.Disposals;
}

/// <summary>
/// Event raised when determining which direction a disposal holder should head next.
/// </summary>
/// <param name="Holder">The disposal holder.</param>
[ByRefEvent]
public record struct GetDisposalsNextDirectionEvent(Entity<DisposalHolderComponent> Holder)
{
    public Direction Next;
}

/// <summary>
/// The type of disposal tube.
/// </summary>
/// <remarks>Only disposal tubes of same type may connect with each other.</remarks>
[Serializable, NetSerializable]
public enum DisposalTubeType
{
    Disposals,
    Transit
}
