namespace Content.Server.Morgue.Components;

[RegisterComponent]
public sealed class MorgueTrayComponent : Component
{
    [ViewVariables]
    public EntityUid Morgue { get; set; }
}
