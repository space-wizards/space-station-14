using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Nutrition.Components;

/// <summary>
/// This component causes its entity to have movement speed modifiers applied based on the entity's current satiations.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SatiationSpeedModifierSystem))]
public sealed partial class SatiationSpeedModifierComponent : Component
{
    /// <summary>
    /// A dictionary of satiation types to a dictionary containing movement speed modifiers keyed by satiation thresholds.
    /// </summary>
    [DataField(required: true), AutoNetworkedField, IncludeDataField]
    public Dictionary<ProtoId<SatiationTypePrototype>, Dictionary<SatiationValue, float>> Satiations;
}
