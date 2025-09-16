using Content.Shared.Damage;
using Content.Shared.Disposal.Unit;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using System.Text.RegularExpressions;

namespace Content.Shared.Disposal.Tube;

/// <summary>
/// Basic component for disposal pipes.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Virtual]
public partial class DisposalTubeComponent : Component
{
    public static readonly Regex TagRegex = new("^[a-zA-Z0-9, ]*$", RegexOptions.Compiled);

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
    /// or if the angular difference between the entry and potential exit is less than <see cref="MinDeltaAngle"/>.
    /// </remarks>
    [DataField]
    public Direction[] Exits = { Direction.South };

    /// <summary>
    /// The smallest angle that entities can turn while traveling through the conduit.
    /// Only applies when there are more than two potential exits.
    /// </summary>
    [DataField]
    public Angle MinDeltaAngle = 0;

    /// <summary>
    /// Determines the type of disposal pipe -
    /// only pipes of the same type can connect to each other.
    /// </summary>
    [DataField]
    public DisposalTubeType DisposalTubeType = DisposalTubeType.Disposals;
}

[ByRefEvent]
public record struct GetDisposalsNextDirectionEvent(DisposalHolderComponent Holder)
{
    public Direction Next;
}

[Serializable, NetSerializable]
public enum DisposalTubeType
{
    Disposals,
    Transit
}

[Serializable, NetSerializable]
public enum DisposalTubeVisuals
{
    VisualState
}

[Serializable, NetSerializable]
public enum DisposalUiAction
{
    Ok
}

[Serializable, NetSerializable]
public enum DisposalTubeVisualState
{
    Free = 0,
    Anchored,
}
