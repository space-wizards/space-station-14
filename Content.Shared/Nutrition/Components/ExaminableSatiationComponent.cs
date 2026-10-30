using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Nutrition.Components;

/// <summary>
/// Component which specifies what <see cref="ExaminableSatiationComponent"/> shows in examine descriptions.
/// </summary>
/// <seealso cref="ExaminableSatiationSystem"/>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(ExaminableSatiationSystem))]
public sealed partial class ExaminableSatiationComponent : Component
{
    /// <summary>
    /// Examination localization string IDs per threshold per satiation type.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public Dictionary<ProtoId<SatiationTypePrototype>, SatiationThresholds<LocId?>> Satiations;
}
