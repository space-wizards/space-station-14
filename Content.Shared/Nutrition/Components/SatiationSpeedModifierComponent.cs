using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Nutrition.Components;

/// <summary>
/// This component causes its entity to have movement speed modifiers applied based on the entity's current satiations.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true),
 Access(typeof(SatiationSpeedModifierSystem))]
public sealed partial class SatiationSpeedModifierComponent : Component
{
    /// <summary>
    /// Speed modifiers, expressed as <see cref="float"/>s, contained within <see cref="SatiationThresholds{T}"/>,
    /// keyed by the <see cref="SatiationTypePrototype">satiation types</see> they are thresholds for.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public Dictionary<ProtoId<SatiationTypePrototype>, SatiationThresholds<float>> Satiations = [];
}
