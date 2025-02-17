// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;

namespace Content.Shared.DeadSpace.Abilities.AutoInjectReagent;

public abstract class SharedReagentSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();
    }
    public void Inject(Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> reagents, EntityUid target)
    {
        foreach (var reagentEntry in reagents)
        {
            if (reagentEntry.Value <= 0)
                return;

            if (!HasComp<BodyComponent>(target))
                return;

            if (!_solutionContainer.TryGetInjectableSolution(target, out var injectable, out _))
                return;

            _solutionContainer.TryAddReagent(injectable.Value, reagentEntry.Key, reagentEntry.Value, out _);
        }

        return;
    }
}
