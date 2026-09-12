using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Medical.BiomassReclaimer;

/// <summary>
/// Component applied by <see cref="BiomassReclaimerComponent"/> while active.
/// </summary>
[RegisterComponent]
public sealed partial class ActiveBiomassReclaimerComponent : Component
{
    /// <summary>
    /// Status effect that's applied for component lifetime.
    /// </summary>
    [DataField]
    public EntProtoId<StatusEffectComponent> ActiveStatus = "StatusEffectActiveBiomassReclaimer";
}
