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
        SubscribeLocalEvent<ContainedSolutionComponent, ComponentShutdown>(OnComponentShutdown);
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
        if (!Resolve(uid, ref meta))
            throw new InvalidOperationException("Attempted to ensure solution on invalid entity.");

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
            prototype ??= new() { MaxVolume = minVol };
            prototype.Name = name;
            (solutionId, solutionComp, _) = SpawnSolutionUninitialized(solutionSlot, name, minVol, prototype);
            existed = false;
            needsInit = true;
            Dirty(uid, container);
        }
        else
        {
            solutionComp = Comp<SolutionComponent>(solutionId);
            DebugTools.Assert(TryComp(solutionId, out ContainedSolutionComponent? relation) && relation.Container == uid && relation.ContainerName == name);
            DebugTools.Assert(solutionComp.Solution.Name == name);

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


    private Entity<SolutionComponent, ContainedSolutionComponent> SpawnSolutionUninitialized(ContainerSlot container, string name, FixedPoint2 minVol, Solution prototype)
    {
        var coords = new EntityCoordinates(container.Owner, Vector2.Zero);
        var uid = EntityManager.CreateEntityUninitialized(null, coords, null);

        var solution = new SolutionComponent() { Solution = prototype };
        AddComp(uid, solution);

        var relation = new ContainedSolutionComponent() { Container = container.Owner, ContainerName = name };
        AddComp(uid, relation);

        ContainerSystem.Insert(uid, container, force: true);

        return (uid, solution, relation);
    }


    #region Event Handlers

    private void OnMapInit(Entity<SolutionContainerManagerComponent> entity, ref MapInitEvent args)
    {
        if (entity.Comp.Solutions is not { } prototypes)
            return;

        foreach (var (name, prototype) in prototypes)
        {
            EnsureSolutionEntity((entity.Owner, entity.Comp), name, prototype.MaxVolume, prototype, out _);
        }

        entity.Comp.Solutions = null;
        Dirty(entity);
    }

    private void OnComponentShutdown(Entity<SolutionContainerManagerComponent> entity, ref ComponentShutdown args)
    {
        foreach (var name in entity.Comp.Containers)
        {
            if (ContainerSystem.TryGetContainer(entity, $"solution@{name}", out var solutionContainer))
                ContainerSystem.ShutdownContainer(solutionContainer);
        }
        entity.Comp.Containers.Clear();
    }

    private void OnComponentShutdown(Entity<ContainedSolutionComponent> entity, ref ComponentShutdown args)
    {
        if (TryComp(entity.Comp.Container, out SolutionContainerManagerComponent? container))
        {
            container.Containers.Remove(entity.Comp.ContainerName);
            Dirty(entity.Comp.Container, container);
        }

        if (ContainerSystem.TryGetContainer(entity, $"solution@{entity.Comp.ContainerName}", out var solutionContainer))
            ContainerSystem.ShutdownContainer(solutionContainer);
    }

    #endregion Event Handlers
}
