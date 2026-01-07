// SPDX-FileCopyrightText: 2025 terkala <appleorange64@gmail.com>
//
// SPDX-License-Identifier: MIT

using Content.Shared.BloodCult.Components;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.BloodCult.EntityEffects.Effects;

/// <summary>
/// Entity effect that deletes the target entity when triggered.
/// Used for cleaning blood cult runes
/// Only deletes basic runes (not tear veil or final summoning runes).
/// </summary>
[UsedImplicitly]
public sealed partial class DeleteEntityEffect : EntityEffectBase<DeleteEntityEffect>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return null; // Not shown in guidebook
    }
}

/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
[UsedImplicitly]
public sealed partial class DeleteEntityEffectSystem : EntityEffectSystem<CleanableRuneComponent, DeleteEntityEffect>
{
    protected override void Effect(Entity<CleanableRuneComponent> entity, ref EntityEffectEvent<DeleteEntityEffect> args)
    {
        // Only delete basic runes (not tear veil or final summoning runes)
        if (HasComp<TearVeilComponent>(entity) ||
            HasComp<FinalSummoningRuneComponent>(entity))
        {
            return;
        }

        // Delete the target entity
        QueueDel(entity);
    }
}
