namespace Content.Shared.Xenoarchaeology.Equipment.Components;

[RegisterComponent]
[Access(typeof(SharedArtifactNukerSystem))]
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
}
