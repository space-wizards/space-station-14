// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

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
}
