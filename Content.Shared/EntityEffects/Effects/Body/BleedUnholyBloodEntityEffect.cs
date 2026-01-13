// SPDX-FileCopyrightText: 2025 Terkala <appleorange64@gmail.com>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Body;

/// <summary>
/// Makes an entity bleed Unholy Blood instead of their normal blood type while they metabolize Edge Essentia.
/// Changes what blood they bleed out, not their internal blood.
/// </summary>
public sealed partial class BleedUnholyBlood : EntityEffectBase<BleedUnholyBlood>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-bleed-unholy-blood", ("chance", Probability));
}
