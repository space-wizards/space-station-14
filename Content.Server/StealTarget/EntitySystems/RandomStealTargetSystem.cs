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

    private void OnInit(EntityUid uid, RandomStealTargetComponent component, ComponentInit args)
    {
        if (component.StealTargetNames.Count == 0)
            return;

        EnsureComp<StealTargetComponent>(uid, out var stealTarget);

        stealTarget.StealGroup = _random.Pick(component.StealTargetNames);
    }

}
