using Content.Server.StationEvents.Events;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(RandomSentienceRule))]
public sealed partial class SentienceTargetComponent : Component
{
    [DataField(required: true)]
    public string FlavorKind = default!;

    [DataField]
    public float Weight = 1.0f;
}
