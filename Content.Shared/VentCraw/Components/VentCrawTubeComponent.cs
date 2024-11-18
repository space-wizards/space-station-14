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
