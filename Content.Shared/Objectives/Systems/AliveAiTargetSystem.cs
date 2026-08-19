using System.Diagnostics.CodeAnalysis;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Silicons.StationAi;

namespace Content.Shared.Objectives.Systems;

/// <summary>
/// This filters for specific minds form
/// </summary>
public sealed partial class AliveAiTargetSystem : MindTargetSystem<StationAiCoreComponent, StationAiHolderComponent>
{
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedStationAiSystem _ai = default!;

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

public sealed partial class AliveAiPool : MindPool<AliveAiTargetSystem>;
