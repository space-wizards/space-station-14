namespace Content.Server.Explosion.Components;

[RegisterComponent]
public sealed partial class TriggerOnSmokableExpendedComponent : Component
{
    /// <summary>
    /// The entity which lit the triggering expendable entity
    /// </summary>
    [DataField]
    public EntityUid? LitBy = default!;
}
