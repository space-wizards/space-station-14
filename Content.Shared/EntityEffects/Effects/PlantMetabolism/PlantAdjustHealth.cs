// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

namespace Content.Shared.EntityEffects.Effects.PlantMetabolism;

public sealed partial class PlantAdjustHealth : PlantAdjustAttribute<PlantAdjustHealth>
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-health";
}

