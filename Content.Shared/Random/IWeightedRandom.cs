using Robust.Shared.Prototypes;

namespace Content.Shared.Random;

/// <summary>
/// IWeightedRandomPrototype implements a dictionary of strings to float weights
/// to be used with <see cref="Helpers.SharedRandomExtensions.Pick(IWeightedRandomPrototype, Robust.Shared.Random.IRobustRandom)" />.
/// </summary>
public interface IWeightedRandomPrototype : IPrototype
{
    [ViewVariables]
    public Dictionary<string, float> Weights { get; }
}
