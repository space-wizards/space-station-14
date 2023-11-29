using Content.Server.Chemistry.Containers.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Utility;
using System.Numerics;

namespace Content.Server.Chemistry.Containers.EntitySystems;

public sealed partial class SolutionContainerSystem : SharedSolutionContainerSystem
{
    [Dependency] private readonly INetManager _netManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SolutionContainerManagerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SolutionContainerComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<ContainerSolutionComponent, ComponentShutdown>(OnComponentShutdown);
    }

    public IEnumerable<(string Name, Solution Solution)> EnumerateSolutions(SolutionContainerManagerComponent container)
    {
        foreach (var (name, solution) in container.Solutions)
        {
            yield return (name, solution);
        }
    }


    public Solution EnsureSolution(Entity<MetaDataComponent?> entity, string name)
        => EnsureSolution(entity, name, out _);

    public Solution EnsureSolution(Entity<MetaDataComponent?> entity, string name, out bool existed)
        => EnsureSolution(entity, name, FixedPoint2.Zero, out existed);

    public Solution EnsureSolution(Entity<MetaDataComponent?> entity, string name, FixedPoint2 minVol, out bool existed)
    {
        var (uid, meta) = entity;
        DebugTools.Assert(Resolve(uid, ref meta), $"Attempted to ensure solution on invalid entity {ToPrettyString(entity.Owner)}");

        if (meta.EntityLifeStage >= EntityLifeStage.MapInitialized)
            return EnsureSolutionEntity(uid, name, minVol, out existed).Comp.Solution;
        else
            return EnsureSolutionPrototype(uid, name, minVol, out existed);
    }

    public Entity<SolutionComponent> EnsureSolutionEntity(Entity<SolutionContainerComponent?> entity, string name, FixedPoint2 minVol, out bool existed)
    {
        existed = true;

        var (uid, container) = entity;
        if (!Resolve(uid, ref container, logMissing: false))
        {
            container = AddComp<SolutionContainerComponent>(uid);
            existed = false;
        }

        if (!container.Solutions.TryGetValue(name, out var solutionSlot))
        {
            solutionSlot = ContainerSystem.EnsureContainer<ContainerSlot>(entity.Owner, $"solution@{name}");
            container.Solutions.Add(name, solutionSlot);
            Dirty(uid, container);
        }

        var needsInit = false;
        SolutionComponent solutionComp;
        if (solutionSlot.ContainedEntity is not { } solutionId)
        {
            (solutionId, solutionComp) = SpawnSolutionUninitialized(uid, name, new Solution() { MaxVolume = minVol, Name = name });
            ContainerSystem.Insert(solutionId, solutionSlot, force: true);
            existed = false;
            needsInit = true;
        }
        else if (!TryComp(solutionId, out solutionComp!))
        {
            solutionComp = new SolutionComponent()
            {
                Solution = new Solution() { MaxVolume = minVol, Name = name },
            };
            AddComp(solutionId, solutionComp);
            existed = false;
        }
        else
        {
            solutionComp.Solution.MaxVolume = FixedPoint2.Max(solutionComp.Solution.MaxVolume, minVol);
            Dirty(solutionId, solutionComp);
        }

        if (!TryComp(solutionId, out ContainerSolutionComponent? relation))
        {
            relation = new ContainerSolutionComponent() { Container = uid, Name = name };
            AddComp(solutionId, relation);
        }
        else
        {
            (relation.Container, relation.Name) = (uid, name);
            Dirty(solutionId, relation);
        }

        if (needsInit)
            EntityManager.InitializeAndStartEntity(solutionId, Transform(solutionId).MapID);

        return (solutionId, solutionComp);
    }

    private Solution EnsureSolutionPrototype(Entity<SolutionContainerManagerComponent?> entity, string name, FixedPoint2 minVol, out bool existed)
    {
        existed = true;

        var (uid, container) = entity;
        if (!Resolve(uid, ref container, logMissing: false))
        {
            container = AddComp<SolutionContainerManagerComponent>(uid);
            existed = false;
        }

        if (!container.Solutions.TryGetValue(name, out var prototype))
        {
            prototype = new Solution() { MaxVolume = minVol, Name = name };
            container.Solutions.Add(name, prototype);
            existed = false;
        }
        else
            prototype.MaxVolume = FixedPoint2.Max(prototype.MaxVolume, minVol);

        return prototype;
    }


    private Entity<SolutionComponent> SpawnSolutionUninitialized(EntityUid container, string name, Solution prototype)
    {
        var coords = new EntityCoordinates(container, Vector2.Zero);
        var uid = EntityManager.CreateEntityUninitialized(null, coords, null);
        if (!TryComp(uid, out SolutionComponent? comp))
        {
            var solution = prototype.Clone();
            solution.Name = name;
            comp = new SolutionComponent() { Solution = solution };
            AddComp(uid, comp);
        }

        if (!TryComp(uid, out ContainerSolutionComponent? relation))
        {
            relation = new ContainerSolutionComponent() { Container = container, Name = name };
            AddComp(uid, relation);
        }
        else
            (relation.Container, relation.Name) = (container, name);

        return (uid, comp);
    }


    #region Event Handlers

    private void OnMapInit(EntityUid uid, SolutionContainerManagerComponent comp, MapInitEvent args)
    {
        if (comp.Solutions is { Count: > 0 } prototypes)
        {
            foreach (var (name, prototype) in prototypes)
            {
                var solution = EnsureSolutionEntity(uid, name, prototype.MaxVolume, out _);
                SolutionSystem.AddSolution(solution, prototype);
            }
        }

        RemComp(uid, comp);
    }

    private void OnComponentShutdown(EntityUid uid, SolutionContainerComponent comp, ComponentShutdown args)
    {
        while (comp.Solutions.FirstOrNull() is { } solution)
        {
            solution.Value.Shutdown(EntityManager, _netManager);
            comp.Solutions.Remove(solution.Key);
        }
    }

    private void OnComponentShutdown(EntityUid uid, ContainerSolutionComponent comp, ComponentShutdown args)
    {
        if (!TryComp(comp.Container, out SolutionContainerComponent? container))
            return;

        container.Solutions.Remove(comp.Name);
        Dirty(comp.Container, container);
    }

    #endregion Event Handlers
}
