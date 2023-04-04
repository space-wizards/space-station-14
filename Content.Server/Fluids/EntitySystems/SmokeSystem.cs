using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Fluids.EntitySystems;

/// <summary>
/// Handles non-atmos solution entities similar to puddles.
/// </summary>
public sealed class SmokeSystem : EntitySystem
{
    // If I could do it all again this could probably use a lot more of puddles.
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SmokeComponent, EntityUnpausedEvent>(OnSmokeUnpaused);
        SubscribeLocalEvent<SmokeComponent, MapInitEvent>(OnSmokeMapInit);
    }

    private void OnSmokeMapInit(EntityUid uid, SmokeComponent component, MapInitEvent args)
    {
        component.NextReact = _timing.CurTime;
    }

    private void OnSmokeUnpaused(EntityUid uid, SmokeComponent component, ref EntityUnpausedEvent args)
    {
        component.NextReact += args.PausedTime;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<SmokeComponent>();
        var curTime = _timing.CurTime;

        while (query.MoveNext(out var uid, out var smoke))
        {
            if (smoke.NextReact > curTime)
                continue;

            smoke.NextReact += TimeSpan.FromSeconds(1.5);

            SmokeReact(uid, 1f, smoke);
        }
    }

    public void SmokeReact(EntityUid uid, float averageExposures, SmokeComponent? component = null, TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref component, ref xform))
            return;

        if (!_solutionSystem.TryGetSolution(uid, SmokeComponent.SolutionName, out var solution) ||
            solution.Contents.Count == 0)
        {
            return;
        }

        if (!_mapManager.TryGetGrid(xform.GridUid, out var mapGrid))
            return;

        var tile = mapGrid.GetTileRef(xform.Coordinates.ToVector2i(EntityManager, _mapManager));
        var chemistry = _entities.EntitySysManager.GetEntitySystem<ReactiveSystem>();
        var lookup = _entities.EntitySysManager.GetEntitySystem<EntityLookupSystem>();

        var solutionFraction = 1 / Math.Floor(averageExposures);
        var ents = lookup.GetEntitiesIntersecting(tile, LookupFlags.Uncontained).ToArray();

        foreach (var reagentQuantity in solution.Contents.ToArray())
        {
            if (reagentQuantity.Quantity == FixedPoint2.Zero)
                continue;

            var reagent = PrototypeManager.Index<ReagentPrototype>(reagentQuantity.ReagentId);

            // React with the tile the effect is on
            // We don't multiply by solutionFraction here since the tile is only ever reacted once
            if (!component.ReactedTile)
            {
                reagent.ReactionTile(tile, reagentQuantity.Quantity);
                component.ReactedTile = true;
            }

            // Touch every entity on the tile
            foreach (var entity in ents)
            {
                chemistry.ReactionEntity(entity, ReactionMethod.Touch, reagent,
                    reagentQuantity.Quantity * solutionFraction, solution);
            }
        }

        foreach (var entity in ents)
        {
            ReactWithEntity(entity, solutionFraction);
        }
    }

    private void UpdateVisuals(EntityUid uid, SmokeSolutionAreaEffectComponent)
    {
        if (TryComp(Owner, out AppearanceComponent? appearance) &&
            _solutionSystem.TryGetSolution(Owner, SolutionName, out var solution))
        {
            appearance.SetData(SmokeVisuals.Color, solution.GetColor(_proto));
        }
    }

    private void ReactWithEntity(EntityUid entity, double solutionFraction)
    {
        if (!EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner, SolutionName, out var solution))
            return;

        if (!_entMan.TryGetComponent(entity, out BloodstreamComponent? bloodstream))
            return;

        if (_entMan.TryGetComponent(entity, out InternalsComponent? internals) &&
            IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<InternalsSystem>().AreInternalsWorking(internals))
            return;

        var chemistry = EntitySystem.Get<ReactiveSystem>();
        var cloneSolution = solution.Clone();
        var transferAmount = FixedPoint2.Min(cloneSolution.Volume * solutionFraction, bloodstream.ChemicalSolution.AvailableVolume);
        var transferSolution = cloneSolution.SplitSolution(transferAmount);

        foreach (var reagentQuantity in transferSolution.Contents.ToArray())
        {
            if (reagentQuantity.Quantity == FixedPoint2.Zero) continue;
            chemistry.ReactionEntity(entity, ReactionMethod.Ingestion, reagentQuantity.ReagentId, reagentQuantity.Quantity, transferSolution);
        }

        var bloodstreamSys = EntitySystem.Get<BloodstreamSystem>();
        if (bloodstreamSys.TryAddToChemicals(entity, transferSolution, bloodstream))
        {
            // Log solution addition by smoke
            _adminLogger.Add(LogType.ForceFeed, LogImpact.Medium, $"{_entMan.ToPrettyString(entity):target} was affected by smoke {SolutionContainerSystem.ToPrettyString(transferSolution)}");
        }
    }
}
