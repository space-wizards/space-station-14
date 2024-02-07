using Content.Shared.Chemistry.EntitySystems;

namespace Content.Shared.Chemistry.Components;

[RegisterComponent, Access(typeof(SolutionSpikerSystem))]
public sealed partial class SolutionSpikerComponent : Component
{
    /// <summary>
    ///     The source solution to take the reagents from in order
    ///     to spike the other solution container.
    /// </summary>
    [DataField(required: true)]
    public string SourceSolution = string.Empty;

    /// <summary>
    ///     If spiking with this entity should ignore empty containers or not.
    /// </summary>
    [DataField]
    public bool IgnoreEmpty;

    /// <summary>
    ///     What should pop up when spiking with this entity.
    /// </summary>
    [DataField]
    public LocId Popup = "spike-solution-generic";

    /// <summary>
    ///     What should pop up when spiking fails because the container was empty.
    /// </summary>
    [DataField]
    public LocId PopupEmpty = "spike-solution-empty-generic";
}
