using Content.Shared.Body.Events;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction.Systems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Digestion.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Medical.Digestion.Systems;

public sealed partial class DigestionSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionSystem = default!;
    [Dependency] private readonly ChemicalRateReactionSystem _rateReactionSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DigestionComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<DigestionComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<DigestionComponent, BodyInitializedEvent>(OnBodyInit);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<DigestionComponent, SolutionContainerManagerComponent>();
        while (query.MoveNext(out var uid, out var digester, out var solMan))
        {
            if (_timing.CurTime < digester.LastUpdate + digester.UpdateRate)
                continue;
            UpdateReactions((uid, digester),
                (digester.CachedDigestionSolution,
                Comp<SolutionComponent>(digester.CachedDigestionSolution)));
            digester.LastUpdate = _timing.CurTime;
        }
    }

private void OnBodyInit(Entity<DigestionComponent> digester, ref BodyInitializedEvent args)
    {
        if (!digester.Comp.UseBodySolution)
            return;
        UpdateCachedSolutions(digester, args.Body);
    }

    private void OnCompInit(Entity<DigestionComponent> digester, ref ComponentInit args)
    {
        if (digester.Comp.DissolvingReagent != null)
        {
            digester.Comp.CachedDissolverReagent = new ReagentId(digester.Comp.DissolvingReagent.Value, null);
        }
        UpdateCachedPrototypes(digester);
    }

    private void OnMapInit(Entity<DigestionComponent> digester, ref MapInitEvent args)
    {
        if (_netManager.IsClient)
            return;
        if (digester.Comp.UseBodySolution)
            return;
        UpdateCachedSolutions(digester, null);
    }


    private void UpdateCachedSolutions(Entity<DigestionComponent> digester, EntityUid? absorbSolutionOwner)
    {
        absorbSolutionOwner ??= digester;
        if (!_solutionSystem.EnsureSolutionEntity((digester, null),
                DigestionComponent.DigestingSolutionId,
                out var digestionSolEnt,
                FixedPoint2.MaxValue))
        {
            Log.Error($"Could not ensure solution {DigestionComponent.DigestingSolutionId} on {ToPrettyString(digester)}." +
                      $"If this is being run on the client make sure it runs AFTER mapInit!");
            return;
        }
        digester.Comp.CachedDigestionSolution = digestionSolEnt.Value;
        if (!_solutionSystem.TryGetSolution((absorbSolutionOwner.Value, null),
                digester.Comp.AbsorberSolutionId,
                out var absorbSolEnt,
                true))
            return;
        digester.Comp.CachedDigestionSolution = absorbSolEnt.Value;
        Dirty(digester);
    }


}
