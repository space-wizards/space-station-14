using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Xenoarchaeology.Equipment.Components;

/// <summary>
/// Marks an entity the target of an artifact crusher that is currently crushing it.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
[Access(typeof(SharedArtifactCrusherSystem))]
public sealed partial class ArtifactCrusherTargetComponent : Component
{
    /// <summary>
    /// The crusher entity that is currently crushing the target.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Crusher;
}
