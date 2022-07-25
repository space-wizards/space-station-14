namespace Content.Shared.Ensnaring.Components;
/// <summary>
/// Use this on something you want to use to ensnare an entity with
/// </summary>
public abstract class SharedEnsnaringComponent : Component
{
    /// <summary>
    /// How long it should take to free someone.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("freeTime")]
    public float freeTime = 6f;

    /// <summary>
    /// How long it should take for an entity to free themselves.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("breakoutTime")]
    public float BreakoutTime = 10f;

    //TODO: Raise default value, make datafield required.
    /// <summary>
    /// How much should this slow down the entities walk?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("walkSpeed")]
    public float WalkSpeed = 0.3f;

    //TODO: Raise default value, make datafield required.
    /// <summary>
    /// How much should this slow down the entities sprint?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("sprintSpeed")]
    public float SprintSpeed = 0.3f;

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

public sealed class EnsnareAttemptEvent : CancellableEntityEventArgs
{
    //TODO: Ensnare targeting logic here
    //Might be better off changed to an unensnare event
}

public sealed class EnsnareChangeEvent : EntityEventArgs
{
    //TODO: Ensnare change logic here. Needs to relay movespeed info.

    public readonly EntityUid User;
    public readonly float WalkSpeed;
    public readonly float SprintSpeed;

    public EnsnareChangeEvent(EntityUid user, float walkSpeed, float sprintSpeed)
    {
        User = user;
        WalkSpeed = walkSpeed;
        SprintSpeed = sprintSpeed;
    }
}
