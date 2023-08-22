namespace Content.Server.Chemistry.Components;

[RegisterComponent]
public sealed partial class SolutionSpikerComponent : Component
{
    /// <summary>
    ///     The source solution to take the reagents from in order
    ///     to spike the other solution container.
    /// </summary>
    [DataField("sourceSolution")]
    public string SourceSolution { get; private set; } = string.Empty;

    /// <summary>
    ///     If spiking with this entity should ignore empty containers or not.
    /// </summary>
    [DataField("ignoreEmpty")]
    public bool IgnoreEmpty { get; private set; }

    /// <summary>
    ///     What should pop up when spiking with this entity.
    /// </summary>
    [DataField("popup")]
    public string Popup { get; private set; } = "spike-solution-generic";

    /// <summary>
    ///     What should pop up when spiking fails because the container was empty.
    /// </summary>
    [DataField("popupEmpty")]
    public string PopupEmpty { get; private set; } = "spike-solution-empty-generic";
}
