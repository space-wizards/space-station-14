namespace Content.Server.StationEvents.Components;

[RegisterComponent]
public sealed class SentienceTargetComponent : Component
{
    [DataField("flavorKind", required: true)]
    public string FlavorKind = default!;
}
