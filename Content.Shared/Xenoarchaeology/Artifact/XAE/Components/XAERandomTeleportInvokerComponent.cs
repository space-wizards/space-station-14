using Robust.Shared.GameStates;

namespace Content.Shared.Xenoarchaeology.Artifact.XAE.Components;

/// <summary>
/// When activated, will teleport the artifact
/// to a random position within a certain radius
/// </summary>
[RegisterComponent, Access(typeof(XAERandomTeleportInvokerSystem)), NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XAERandomTeleportInvokerComponent : Component
{
    /// <summary>
    /// The max distance that the artifact will teleport.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxRange = 15f;

    /// <summary>
    /// The min distance that the artifact will teleport.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MinRange = 6f;
}
