using Content.Server.Body.Components;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffects;

public sealed partial class ModifyLungGas : ReagentEffect
{
    [DataField("ratios", required: true)]
    private Dictionary<Gas, float> _ratios = default!;

    // JUSTIFICATION: This is internal magic that players never directly interact with.
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;

    public override void Effect(ReagentEffectArgs args)
    {
        if (args.EntityManager.TryGetComponent<LungComponent>(args.OrganEntity, out var lung))
        {
            foreach (var (gas, ratio) in _ratios)
            {
                lung.Air.Moles[(int) gas] += (ratio * args.Quantity.Float()) / Atmospherics.BreathMolesToReagentMultiplier;
            }
        }
    }
}
