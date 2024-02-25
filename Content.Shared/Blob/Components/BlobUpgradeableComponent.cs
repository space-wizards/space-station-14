using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Blob.Components;

/// <summary>
/// This is used for a blob tile that can be upgraded into another type of tile
/// by a blob marker at the cost of some resources.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedBlobSystem))]
public sealed partial class BlobUpgradeableComponent : Component
{
    /// <summary>
    /// The entity that is spawned when this is upgraded.
    /// </summary>
    [DataField]
    public EntProtoId UpgradeEntity;

    /// <summary>
    /// The amount of resource it takes to upgrade this entity.
    /// </summary>
    [DataField]
    public int UpgradeCost = 15;
}
