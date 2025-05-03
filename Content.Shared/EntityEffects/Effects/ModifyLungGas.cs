using Content.Shared.Atmos;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

public sealed partial class ModifyLungGas : EventEntityEffect<ModifyLungGas>
{
    [DataField("ratios", required: true)]
    public Dictionary<Gas, float> Ratios = default!;

    // JUSTIFICATION: This is internal magic that players never directly interact with.
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}
