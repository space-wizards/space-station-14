namespace Content.Server.Chemistry.Components;

[RegisterComponent]
public sealed class SolutionSpikerComponent : Component
{
    /// <summary>
    ///     The source solution to take the reagents from in order
    ///     to spike the other solution container.
    /// </summary>
    [DataField("sourceSolution")]
    public string SourceSolution { get; } = string.Empty;

    /// <summary>
    ///     If spiking with this entity should ignore empty containers or not.
    /// </summary>
    [DataField("ignoreEmpty")]
    public bool IgnoreEmpty { get; }
}
