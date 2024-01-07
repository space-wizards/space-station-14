using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

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
    /// <summary>
    ///     A list of components that will be searched for when selected from the verb menu.
    ///     The closest entities found with one of the components in this list will be added to the StoredTargets.
    /// </summary>
    [DataField("components"), ViewVariables(VVAccess.ReadWrite)]
    public List<string> Components = new();

    /// <summary>
    ///     A list of entities that are stored on the pinpointer
    /// </summary>
    [DataField("storedTargets"), ViewVariables(VVAccess.ReadWrite)]
    public List<EntityUid> StoredTargets = new();

    /// <summary>
    ///     The maximum amount of targets the pinpointer is able to store
    /// </summary>
    [DataField("maxTargets"), ViewVariables(VVAccess.ReadWrite)]
    public int MaxTargets = 10;

    [DataField("mediumDistance"), ViewVariables(VVAccess.ReadWrite)]
    public float MediumDistance = 16f;

    [DataField("closeDistance"), ViewVariables(VVAccess.ReadWrite)]
    public float CloseDistance = 8f;

    [DataField("reachedDistance"), ViewVariables(VVAccess.ReadWrite)]
    public float ReachedDistance = 1f;

    /// <summary>
    ///     Pinpointer arrow precision in radians.
    /// </summary>
    [DataField("precision"), ViewVariables(VVAccess.ReadWrite)]
    public double Precision = 0.09;

    /// <summary>
    ///     Name to display of the target being tracked.
    /// </summary>
    [DataField("targetName"), ViewVariables(VVAccess.ReadWrite)]
    public string? TargetName;

    /// <summary>
    ///     Whether or not the target name should be updated when the target is updated.
    /// </summary>
    [DataField("updateTargetName"), ViewVariables(VVAccess.ReadWrite)]
    public bool UpdateTargetName = true;

    /// <summary>
    ///     Whether or not the target can be reassigned.
    /// </summary>
    [DataField("canRetarget"), ViewVariables(VVAccess.ReadWrite)]
    public bool CanRetarget;

    [DataField("componentNames"), ViewVariables(VVAccess.ReadWrite)]
    public List<string> ComponentNames = new();

    [ViewVariables]
    public EntityUid? Target = null;

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
