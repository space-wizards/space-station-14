using Robust.Shared.Random;
using Content.Server.Objectives.Components.Targets;

namespace Content.Shared.ImportantDocument;

public sealed class RandomStealTargetSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RandomStealTargetComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<RandomStealTargetComponent> entity, ref ComponentInit args)
    {
        if (entity.Comp.StealTargetNames.Count == 0)
            return;

        EnsureComp<StealTargetComponent>(entity, out var stealTarget);

        stealTarget.StealGroup = _random.Pick(entity.Comp.StealTargetNames);
    }

}
