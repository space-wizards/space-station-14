using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;

namespace Content.Shared.Pinpointer;

/// <summary>
/// Displays a sprite on the item that points towards the target component.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
[Access(typeof(SharedPinpointerSystem))]
public sealed partial class PinpointerComponent : Component
{
    // TODO: Type serializer oh god
    [DataField]
    public string? Component;

    [DataField]
    public float MediumDistance = 16f;

    [DataField]
    public float CloseDistance = 8f;

    [DataField]
    public float ReachedDistance = 1f;

    /// <summary>
    ///     Pinpointer arrow precision in radians.
    /// </summary>
    [DataField]
    public double Precision = 0.09;

    /// <summary>
    ///     Whether or not the target can be reassigned.
    /// </summary>
    [DataField]
    public bool CanRetarget;

    /// <summary>
    ///     The pinpointer's target if a target has been specified by a retargeting.
    /// </summary>
    [DataField]
    public PinpointerTarget? Target;

    /// <summary>
    ///     A list of each PinpointerTarget.
    /// </summary>
    [DataField]
    public List<PinpointerTarget> AllTargets;

    [ViewVariables, AutoNetworkedField]
    public bool IsActive = false;

    [ViewVariables, AutoNetworkedField]
    public Angle ArrowAngle;

    [ViewVariables, AutoNetworkedField]
    public Distance DistanceToTarget = Distance.Unknown;

    [ViewVariables]
    public bool HasTarget => DistanceToTarget != Distance.Unknown;
}

[Serializable, NetSerializable]
public enum Distance : byte
{
    Unknown,
    Reached,
    Close,
    Medium,
    Far
}

/// <summary>
///     A target entry.
/// </summary>
public abstract record PinpointerTarget
{
    [DataField(required:true)]
    public required string Name;
}

[DataDefinition, Serializable]
public sealed partial record PinpointerComponentTarget : PinpointerTarget
{
    [DataField(required:true)]
    public required string Target;
}

[DataDefinition, Serializable]
public sealed partial record PinpointerEntityUidTarget : PinpointerTarget
{
    [DataField(required:true)]
    public required EntityUid Target;
}

/// <summary>
///     Search for a specific entity proto id from every entity with a given component.
///     Component should NOT be something highly generic like transform because we will
///     be querying for every entity with that component.
/// </summary>
[DataDefinition, Serializable]
public sealed partial record PinpointerEntProtoIdTarget : PinpointerTarget
{
    [DataField(required:true)]
    public required EntProtoId Target;

    [DataField(required: true)]
    public required string Component;
}
