namespace Content.Shared.Ensnaring.Components;
/// <summary>
/// Use this on something you want to use to ensnare an entity with
/// </summary>
public abstract class SharedEnsnaringComponent : Component
{
    /// <summary>
    /// How long it should take to free someone else.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("freeTime")]
    public float FreeTime = 3.5f;

    /// <summary>
    /// How long it should take for an entity to free themselves.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("breakoutTime")]
    public float BreakoutTime = 30.0f;

    /// <summary>
    /// How much should this slow down the entities walk?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("walkSpeed")]
    public float WalkSpeed = 0.9f;

    /// <summary>
    /// How much should this slow down the entities sprint?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("sprintSpeed")]
    public float SprintSpeed = 0.9f;

    /// <summary>
    /// Should this ensnare someone when thrown?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("canThrowTrigger")]
    public bool CanThrowTrigger;

    /// <summary>
    /// What is ensnared?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("ensnared")]
    public EntityUid? Ensnared;
}

/// <summary>
/// Used whenever you want to do something when someone becomes ensnared by the <see cref="SharedEnsnaringComponent"/>
/// </summary>
public sealed class EnsnareEvent : EntityEventArgs
{
    public readonly float WalkSpeed;
    public readonly float SprintSpeed;

    public EnsnareEvent(float walkSpeed, float sprintSpeed)
    {
        WalkSpeed = walkSpeed;
        SprintSpeed = sprintSpeed;
    }
}

/// <summary>
/// Used whenever you want to do something when someone is freed by the <see cref="SharedEnsnaringComponent"/>
/// </summary>
public sealed class EnsnareRemoveEvent : CancellableEntityEventArgs
{

}
