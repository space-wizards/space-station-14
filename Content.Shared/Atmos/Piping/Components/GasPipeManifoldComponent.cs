namespace Content.Shared.Atmos.Piping.Components;

/// <summary>
/// Marks a pipe manifold with named inlet and outlet pipe nodes.
/// </summary>
[RegisterComponent]
public sealed partial class GasPipeManifoldComponent : Component
{
    /// <summary>
    /// Pipe node names used as manifold inputs.
    /// </summary>
    [DataField("inlets")]
    public HashSet<string> InletNames { get; set; } = new() { "south0", "south1", "south2" };

    /// <summary>
    /// Pipe node names used as manifold outputs.
    /// </summary>
    [DataField("outlets")]
    public HashSet<string> OutletNames { get; set; } = new() { "north0", "north1", "north2" };
}
