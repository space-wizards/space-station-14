namespace Content.Server.Explosion.Components;

[RegisterComponent]
public sealed partial class TriggerOnSmokableExpendedComponent : Component
{
    [DataField]
    public EntityUid? LitBy = default!;
}
