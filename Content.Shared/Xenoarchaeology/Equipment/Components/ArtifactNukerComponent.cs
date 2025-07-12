using Robust.Shared.Serialization;
using Robust.Shared.GameStates;

namespace Content.Shared.Xenoarchaeology.Equipment.Components;

/// <summary>
///     Component that allows objects delete nodes in artifacts on interaction.
///     Related logic lies in the <see cref="SharedArtifactNukerSystem"/>
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
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

    /// <summary>
    ///     Index of the artifact node that will be attempted to nuke.
    /// </summary>
    [ViewVariables]
    [AutoNetworkedField]
    public int? Index;
}

[Serializable, NetSerializable]
public enum ArtifactNukerUiKey : byte
{
    Key
}
