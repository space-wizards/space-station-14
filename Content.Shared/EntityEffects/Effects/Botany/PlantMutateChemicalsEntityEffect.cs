using Content.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Botany;

/// <summary>
/// See serverside system.
/// </summary>
public sealed partial class PlantMutateChemicals : EntityEffectBase<PlantMutateChemicals>
{
    /// <summary>
    /// The Reagent list this mutation draws from.
    /// </summary>
    [DataField]
    public ProtoId<WeightedRandomFillSolutionPrototype> RandomPickBotanyReagent = "RandomPickBotanyReagent";
}
