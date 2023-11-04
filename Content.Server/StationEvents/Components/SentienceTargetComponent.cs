using Content.Server.StationEvents.Events;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(RandomSentienceRule))]
public sealed partial class SentienceTargetComponent : Component
{
    [DataField("flavorKind", required: true)]
    public string FlavorKind = default!;
}
