// SPDX-FileCopyrightText: 2025 Terkala <appleorange64@gmail.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT

using Content.Shared.BloodCult;
using Content.Shared.BloodCult.Components;
using Content.Shared.EntityConditions;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.BloodCult.EntityEffects.EffectConditions;

/// <summary>
/// Condition that checks if an entity is a Blood Cultist.
/// Used for effects that should only affect cultists (like holy smoke).
/// </summary>
/// <inheritdoc cref="EntityConditionSystem{T, TCondition}"/>
[UsedImplicitly]
public sealed partial class IsBloodCultistEntityConditionSystem : EntityConditionSystem<BloodCultistComponent, IsBloodCultist>
{
    protected override void Condition(Entity<BloodCultistComponent> entity, ref EntityConditionEvent<IsBloodCultist> args)
    {
        args.Result = !args.Condition.Invert;
    }
}

/// <inheritdoc cref="EntityCondition"/>
[UsedImplicitly]
public sealed partial class IsBloodCultist : EntityConditionBase<IsBloodCultist>
{
    /// <summary>
    /// If true, invert the result (check if NOT a cultist).
    /// </summary>
    [DataField]
    public bool Invert = false;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return Loc.GetString("reagent-effect-condition-guidebook-is-blood-cultist", ("invert", Invert));
    }
}
