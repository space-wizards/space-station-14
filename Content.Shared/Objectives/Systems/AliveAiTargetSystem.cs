using System.Diagnostics.CodeAnalysis;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Silicons.StationAi;

namespace Content.Shared.Objectives.Systems;

/// <summary>
/// This handles...
/// </summary>
public sealed class AliveAiTargetSystem : MindTargetSystem<StationAiCoreComponent, StationAiHolderComponent>
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedStationAiSystem _ai = default!;

    protected override bool ValidateEntity(Entity<StationAiCoreComponent, StationAiHolderComponent> entity, [NotNullWhen(true)] out Entity<MindComponent>? mind)
    {
        mind = null;
        if (!_ai.TryGetHeld((entity, entity.Comp2), out var held) || _mobState.IsDead(held.Value))
            return false;

        if (!Mind.TryGetMind(held.Value, out var mindId, out var mindComp))
            return false;

        mind = (mindId, mindComp);
        return true;
    }
}

public sealed class AliveHumanoidTargetSystem : MindTargetSystem<HumanoidProfileComponent, MobStateComponent>
{
    [Dependency] private readonly MobStateSystem _mobState = default!;

    protected override bool ValidateEntity(Entity<HumanoidProfileComponent, MobStateComponent> entity, [NotNullWhen(true)] out Entity<MindComponent>? mind)
    {
        mind = null;
        if (!_mobState.IsAlive(entity, entity.Comp2))
            return false;

        if (!Mind.TryGetMind(entity, out var mindId, out var mindComp))
            return false;

        mind = (mindId, mindComp);
        return true;
    }
}
