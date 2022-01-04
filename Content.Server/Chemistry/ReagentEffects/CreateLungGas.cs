using System.Collections.Generic;
using Content.Server.Body.Components;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Chemistry.ReagentEffects;

public class CreateLungGas : ReagentEffect
{
    [DataField("ratios", required: true)]
    private Dictionary<Gas, float> Ratios = default!;

    public override void Effect(ReagentEffectArgs args)
    {
        if (args.EntityManager.TryGetComponent<LungComponent>(args.OrganEntity, out var lung))
        {
            foreach (var (gas, ratio) in Ratios)
            {
                lung.Air.Moles[(int) gas] += (ratio * args.Quantity.Float()) / Atmospherics.BreathMolesToReagentMultiplier;
            }
        }
    }
}
