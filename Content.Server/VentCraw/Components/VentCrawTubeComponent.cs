using Robust.Shared.Containers;

namespace Content.Server.VentCraw.Tube.Components
{
    [RegisterComponent]
    [Access(typeof(VentCrawTubeSystem), typeof(VentCrawableSystem))]
    public sealed class VentCrawTubeComponent : Component
    {
        [DataField("containerId")] public string ContainerId { get; set; } = "VentCrawTube";

        public bool Connected;

        [ViewVariables]
        [Access(typeof(VentCrawTubeSystem), typeof(VentCrawableSystem))]
        public Container Contents { get; set; } = default!;
    }
}

[ByRefEvent]
public record struct GetVentCrawsConnectableDirectionsEvent
{
    public Direction[] Connectable;
}
