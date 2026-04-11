using System.Diagnostics.CodeAnalysis;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;

namespace Content.Shared.Objectives.Systems;

public sealed class AliveHumanoidTargetSystem : MindTargetSystem<MindComponent>
{
    [Dependency] private readonly MobStateSystem _mobState = default!;

    protected override bool ValidateEntity(Entity<MindComponent> entity, [NotNullWhen(true)] out Entity<MindComponent>? mind)
    {
        mind = entity;
        if (entity.Comp.CurrentEntity is not { } humanoid)
            return false;

        return _mobState.IsAlive(humanoid) && HasComp<HumanoidProfileComponent>(humanoid);
    }
}

public sealed partial class AliveHumansPool : MindPool<AliveHumanoidTargetSystem>;
