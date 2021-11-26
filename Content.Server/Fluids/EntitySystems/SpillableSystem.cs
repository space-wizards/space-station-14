using Content.Server.Chemistry.EntitySystems;
using Content.Server.Fluids.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.FixedPoint;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Fluids.EntitySystems;

[UsedImplicitly]
public class SpillableSystem : EntitySystem
{
    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;

    public override void Initialize() 
    {
        base.Initialize();
        SubscribeLocalEvent<SpillableComponent, LandEvent>(SpillOnLand);
        SubscribeLocalEvent<SpillableComponent, GetOtherVerbsEvent>(AddSpillVerb);
    }

    void SpillOnLand(EntityUid uid, SpillableComponent component, LandEvent args) {
        if (args.User != null && _solutionContainerSystem.TryGetSolution(uid, component.SolutionName, out var solutionComponent))
        {
            _solutionContainerSystem
                .Drain(uid, solutionComponent, solutionComponent.DrainAvailable)
                .SpillAt(EntityManager.GetComponent<TransformComponent>(uid).Coordinates, "PuddleSmear");
        }
    }

    private void AddSpillVerb(EntityUid uid, SpillableComponent component, GetOtherVerbsEvent args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!_solutionContainerSystem.TryGetDrainableSolution(args.Target.Uid, out var solution))
            return;

        if (solution.DrainAvailable == FixedPoint2.Zero)
            return;

        Verb verb = new();
        verb.Text = Loc.GetString("spill-target-verb-get-data-text");
        // TODO VERB ICONS spill icon? pouring out a glass/beaker?
        verb.Act = () => _solutionContainerSystem.SplitSolution(args.Target.Uid,
            solution, solution.DrainAvailable).SpillAt(args.Target.Transform.Coordinates, "PuddleSmear");
        verb.Impact = LogImpact.Medium; // dangerous reagent reaction are logged separately.
        args.Verbs.Add(verb);
    }

}
