namespace Content.Server.Objectives.Components;


/// <summary>
///     Will not allow more than the limit of a certain objective type.
/// </summary>
[RegisterComponent, Access(typeof(ObjectiveLimitSystem))]
public sealed partial class LocalObjectiveLimitTypeComponent : Component
{
    /// <summary>
    ///     Max number of the objective type on one mind.
    /// </summary>
    [DataField(required: true)]
    public uint Limit;

    /// <summary>
    ///     The type of objective.
    /// </summary>
    [DataField(required: true)]
    public string ObjectiveType;
}
