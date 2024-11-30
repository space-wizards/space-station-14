// Initial file ported from the Starlight project repo, located at https://github.com/ss14Starlight/space-station-14

using Robust.Shared.Containers;

namespace Content.Shared.VentCraw.Tube.Components;

[RegisterComponent]
public sealed partial class VentCrawTubeComponent : Component
{
    [DataField("containerId")]
    public string ContainerId { get; set; } = "VentCrawTube";

    public bool Connected;

    [ViewVariables]
    public Container Contents { get; set; } = default!;
}

[ByRefEvent]
public record struct GetVentCrawsConnectableDirectionsEvent
{
    public Direction[] Connectable;
}
