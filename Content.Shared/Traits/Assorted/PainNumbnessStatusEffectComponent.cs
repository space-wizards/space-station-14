using Content.Shared.Dataset;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// Hides the damage overlay and displays the health alert for the client controlling the entity as full.
/// Has to be applied as a status effect.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PainNumbnessStatusEffectComponent : Component
{
    /// <summary>
    /// The fluent string prefix to use when picking a random suffix upon taking damage.
    /// This is only active for those who have the pain numbness status effect. Set to null to prevent changing.
    /// </summary>
    [DataField]
    public ProtoId<LocalizedDatasetPrototype>? ForceSayNumbDataset = "ForceSayNumbDataset";
}
