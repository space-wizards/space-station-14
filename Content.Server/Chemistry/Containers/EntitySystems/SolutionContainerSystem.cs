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
        SubscribeLocalEvent<SolutionContainerManagerComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<SolutionContainerComponent, ComponentShutdown>(OnComponentShutdown);
    }


    public Solution EnsureSolution(Entity<MetaDataComponent?> entity, string name)
        => EnsureSolution(entity, name, out _);

    public Solution EnsureSolution(Entity<MetaDataComponent?> entity, string name, out bool existed)
        => EnsureSolution(entity, name, FixedPoint2.Zero, out existed);

    public Solution EnsureSolution(Entity<MetaDataComponent?> entity, string name, FixedPoint2 minVol, out bool existed)
        => EnsureSolution(entity, name, minVol, null, out existed);

    public Solution EnsureSolution(Entity<MetaDataComponent?> entity, string name, FixedPoint2 minVol, Solution? prototype, out bool existed)
    {
        var (uid, meta) = entity;
        DebugTools.Assert(Resolve(uid, ref meta), $"Attempted to ensure solution on invalid entity {ToPrettyString(entity.Owner)}");

        var manager = EnsureComp<SolutionContainerManagerComponent>(uid);
        if (meta.EntityLifeStage >= EntityLifeStage.MapInitialized)
            return EnsureSolutionEntity((uid, manager), name, minVol, prototype, out existed).Comp.Solution;
        else
            return EnsureSolutionPrototype((uid, manager), name, minVol, prototype, out existed);
    }

    public Entity<SolutionComponent> EnsureSolutionEntity(Entity<SolutionContainerManagerComponent?> entity, string name, FixedPoint2 minVol, Solution? prototype, out bool existed)
    {
        existed = true;

        var (uid, container) = entity;

        var solutionSlot = ContainerSystem.EnsureContainer<ContainerSlot>(uid, $"solution@{name}", out existed);
        if (!Resolve(uid, ref container, logMissing: false))
        {
            existed = false;
            container = AddComp<SolutionContainerManagerComponent>(uid);
            container.Containers.Add(name);
        }
        else if (!existed)
        {
            container.Containers.Add(name);
            Dirty(uid, container);
        }

        var needsInit = false;
        SolutionComponent solutionComp;
        if (solutionSlot.ContainedEntity is not { } solutionId)
        {
            (solutionId, solutionComp, _) = SpawnSolutionUninitialized(solutionSlot, name, minVol, prototype ?? new() { Name = name, MaxVolume = minVol });
            existed = false;
            needsInit = true;
            Dirty(uid, container);
        }
        else
        {
            DebugTools.Assert(TryComp(solutionId, out solutionComp!));
            DebugTools.Assert(TryComp(solutionId, out SolutionContainerComponent? relation) && relation.Container == uid && relation.Name == name);

            var solution = solutionComp.Solution;
            solution.MaxVolume = FixedPoint2.Max(solution.MaxVolume, minVol);

            // Depending on MapInitEvent order some systems can ensure solution empty solutions and conflict with the prototype solutions.
            // We want the reagents from the prototype to exist even if something else already created the solution.
            if (prototype is { Volume.Value: > 0 })
                solution.AddSolution(prototype, PrototypeManager);

            Dirty(solutionId, solutionComp);
        }

        if (needsInit)
            EntityManager.InitializeAndStartEntity(solutionId, Transform(solutionId).MapID);

        return (solutionId, solutionComp);
    }

    private Solution EnsureSolutionPrototype(Entity<SolutionContainerManagerComponent?> entity, string name, FixedPoint2 minVol, Solution? prototype, out bool existed)
    {
        existed = true;

        var (uid, container) = entity;
        if (!Resolve(uid, ref container, logMissing: false))
        {
            container = AddComp<SolutionContainerManagerComponent>(uid);
            existed = false;
        }

        if (container.Solutions is null)
            container.Solutions = new(SolutionContainerManagerComponent.DefaultCapacity);

        if (!container.Solutions.TryGetValue(name, out var solution))
        {
            solution = prototype ?? new() { Name = name, MaxVolume = minVol };
            container.Solutions.Add(name, solution);
            existed = false;
        }
        else
            solution.MaxVolume = FixedPoint2.Max(solution.MaxVolume, minVol);

        Dirty(uid, container);
        return solution;
    }


    private Entity<SolutionComponent, SolutionContainerComponent> SpawnSolutionUninitialized(ContainerSlot container, string name, FixedPoint2 minVol, Solution prototype)
    {
        var coords = new EntityCoordinates(container.Owner, Vector2.Zero);
        var uid = EntityManager.CreateEntityUninitialized(null, coords, null);

        var solution = new SolutionComponent() { Solution = prototype };
        AddComp(uid, solution);

        var relation = new SolutionContainerComponent() { Container = container.Owner, Name = name };
        AddComp(uid, relation);

        ContainerSystem.Insert(uid, container, force: true);

        return (uid, solution, relation);
    }


    #region Event Handlers

    private void OnMapInit(EntityUid uid, SolutionContainerManagerComponent comp, MapInitEvent args)
    {
        if (comp.Solutions is not { } prototypes)
            return;

        foreach (var (name, prototype) in prototypes)
        {
            EnsureSolutionEntity(uid, name, prototype.MaxVolume, prototype, out _);
        }
        comp.Solutions = null;
        Dirty(uid, comp);
    }

    private void OnComponentShutdown(EntityUid uid, SolutionContainerManagerComponent comp, ComponentShutdown args)
    {
        foreach (var name in comp.Containers)
        {
            if (ContainerSystem.TryGetContainer(uid, $"solution@{name}", out var solutionContainer))
                solutionContainer.Shutdown(EntityManager, _netManager);
        }
        comp.Containers.Clear();
    }

    private void OnComponentShutdown(EntityUid uid, SolutionContainerComponent comp, ComponentShutdown args)
    {
        if (TryComp(comp.Container, out SolutionContainerManagerComponent? container))
        {
            container.Containers.Remove(comp.Name);
            Dirty(comp.Container, container);
        }

        if (ContainerSystem.TryGetContainer(uid, $"solution@{comp.Name}", out var solutionContainer))
            solutionContainer.Shutdown(EntityManager, _netManager);
    }

    #endregion Event Handlers
}
