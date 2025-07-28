using Content.Shared.Procedural;
using Robust.Shared.Prototypes;

namespace Content.Shared.Salvage.Expeditions.Modifiers;

public interface ISalvageMod
{
    /// <summary>
    /// Player-friendly version describing this modifier.
    /// </summary>
    LocId Description { get; }

    /// <summary>
    /// Cost for difficulty modifiers.
    /// </summary>
    float Cost { get; }

    List<ProtoId<SalvageDifficultyPrototype>>? Difficulties { get; }
}
