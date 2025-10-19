// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

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
