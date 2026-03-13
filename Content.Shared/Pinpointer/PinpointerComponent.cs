using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;

namespace Content.Shared.Pinpointer;

/// <summary>
/// Displays a sprite on the item that points towards a given <see cref="PinpointerTarget"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
[Access(typeof(SharedPinpointerSystem))]
public sealed partial class PinpointerComponent : Component
{
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
    ///     Whether or not additional targets can be added. A pinpointer with this set to true
    ///     is a universal pinpointer.
    /// </summary>
    [DataField]
    public bool CanRetarget;

    /// <summary>
    ///     The pinpointer's target if a target has been specified by a retargeting. Do not define this in YML. If you
    ///     need a pinpointer with a single target, add a single element to the AllTargets list.
    /// </summary>
    public PinpointerTarget? Target;

    /// <summary>
    ///     The current entity we are pointing at. We save this here as opposed to constantly re-getting the entity uid from the
    ///     PinpointerTarget, which may be expensive.
    /// </summary>
    public EntityUid? TargetEntity;

    /// <summary>
    ///     A list of each PinpointerTarget.
    /// </summary>
    [DataField]
    public List<PinpointerTarget> AllTargets = [];

    /// <summary>
    ///     Maximum number of possible targets i.e. max size of AllTargets
    /// </summary>
    [DataField]
    public int TargetLimit = 1;

    /// <summary>
    ///     If the pinpointer is turned on or not.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool IsActive = false;

    [ViewVariables, AutoNetworkedField]
    public Angle ArrowAngle;

    [ViewVariables, AutoNetworkedField]
    public Distance DistanceToTarget = Distance.Unknown;
}

[Serializable, NetSerializable]
public enum Distance : byte
{
    Unknown,
    Reached,
    Close,
    Medium,
    Far,
}

/// <summary>
///     A target entry for a pinpointer.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial record PinpointerTarget
{
    /// <summary>
    ///     The name of the target, to be displayed when examining the pinpointer and when selecting
    ///     a target.
    /// </summary>
    /// <remarks>
    ///     This should almost always be the target's Identity.Name representation.
    /// </remarks>
    [DataField]
    public string? Name;
}

/// <summary>
///     A target entry for the nearest instance of an entity with a specific component.
/// </summary>
public sealed partial record PinpointerComponentTarget : PinpointerTarget
{
    /// <summary>
    ///     A component to search entities for.
    /// </summary>
    [DataField(required: true)]
    public string Target;
}

/// <summary>
///     A target entry for a specific entity.
/// </summary>
public sealed partial record PinpointerEntityUidTarget : PinpointerTarget
{
    [DataField(required: true)]
    public EntityUid Target;
}

/// <summary>
///     A target entry for the nearest instance of an entity with a specific component and
///     a specific EntProtoId.
/// </summary>
public sealed partial record PinpointerEntProtoIdTarget : PinpointerTarget
{
    [DataField(required:true)]
    public EntProtoId Target;

    [DataField(required: true)]
    public string Component;
}
