namespace Content.Server.Atmos.Piping.Components;

[RegisterComponent]
public sealed partial class GasPipeManifoldComponent : Component
{
    [DataField("inlets")]
    public HashSet<string> InletNames { get; set; } = new() { "south0", "south1", "south2" };

    [DataField("outlets")]
    public HashSet<string> OutletNames { get; set; } = new() { "north0", "north1", "north2" };
}
