namespace Content.Shared.Xenoarchaeology.Equipment.Components;

[RegisterComponent]
public sealed partial class ArtifactNukerComponent : Component
{
    /// <summary>
    ///     When true, activates nuked node.
    /// </summary>
    [DataField]
    public bool ActivateNode = true;

    /// <summary>
    ///     Amount of energy that will be drained when nuker used.
    /// </summary>
    [DataField]
    public float EnergyDrain = 50;

    /// <summary>
    ///     Index of the artifact node that will be attempted to nuke.
    /// </summary>
    [ViewVariables]
    public int? Index;
}
