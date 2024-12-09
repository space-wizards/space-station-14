using System.Diagnostics.CodeAnalysis;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.Components.Solutions;
using Content.Shared.Chemistry.Events;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.Containers;

namespace Content.Shared.Chemistry.Systems;

public partial class SharedSolutionSystem
{
    private void ContainersInit()
    {
        SubscribeLocalEvent<SolutionHolderComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<SolutionHolderComponent, EntRemovedFromContainerMessage>(OnRemoved);
    }
    private void OnInserted(Entity<SolutionHolderComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (!SolutionQuery.TryComp(args.Entity, out var solComp))
            return;
        if (args.Container.ID != FormatSolutionContainerId(solComp.Name))
        {
            throw new InvalidOperationException($"Tried to add {ToPrettyString(args.Entity)} without solution component" +
                                                $"to solution container: {ToPrettyString(ent)} with id:{args.Container.ID}");
        }
        solComp.Container = ent;
        solComp.Parent = ent;
        var solution = (args.Entity, solComp);
        ent.Comp.SolutionIds.Add(solComp.Name);
        ent.Comp.Solutions.Add(solution);
        var ev = new SolutionAddedEvent(ent, solution);
        RaiseLocalEvent(ent, ref ev);
        RaiseLocalEvent(args.Entity, ref ev);
    }
    private void OnRemoved(Entity<SolutionHolderComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (!SolutionQuery.TryComp(args.Entity, out var solComp))
            return;
        for (var index = 0; index < ent.Comp.SolutionIds.Count; index++)
        {
            var solutionId = ent.Comp.SolutionIds[index];
            if (solutionId != solComp.Name)
                continue;
            ent.Comp.SolutionIds.RemoveAt(index);
            ent.Comp.Solutions.RemoveAt(index);
            break;
        }
        var ev = new SolutionRemovedEvent(ent, (args.Entity, solComp));
        RaiseLocalEvent(ent, ref ev);
        RaiseLocalEvent(args.Entity, ref ev);
        QueueDel(args.Entity);//Solutions cannot exist outside of solutionHolders
    }

    public bool TryGetSolution(Entity<SolutionHolderComponent?> container,
        string solutionId,
        out Entity<SolutionComponent> solution,
        bool logIfMissing = true)
    {
        if (!Resolve(container, ref container.Comp))
        {
            solution = default;
            if (logIfMissing)
            {
                Log.Error($"Target Entity: {ToPrettyString(container)} Does not have a SolutionHolderComponent " +
                          $"and does not have any solutions!");
            }
            return false;
        }
        for (var i = 0; i < container.Comp.SolutionIds.Count; i++)
        {
            if (container.Comp.SolutionIds[i] != solutionId)
                continue;
            solution = container.Comp.Solutions[i];
            return true;
        }

        if (logIfMissing)
        {
            Log.Error($"Solution with ID: {solutionId}, could not be found in solution containing entity:" +
                      $" {ToPrettyString(container)}");
        }
        solution = default;
        return false;
    }

    public IEnumerable<Entity<SolutionComponent>> EnumerateSolutions(Entity<SolutionHolderComponent?> container)
    {
        if (!Resolve(container, ref container.Comp))
            yield break;
        for (var i = 0; i < container.Comp.SolutionIds.Count; i++)
        {
            yield return container.Comp.Solutions[i];
        }
    }

    public bool ResolveSolution(Entity<SolutionHolderComponent?> container,
        string solutionId,
        ref Entity<SolutionComponent> foundSolution,
        bool logIfMissing = true)
    {
        if (foundSolution.Owner != EntityUid.Invalid)
            return true;
        if (!Resolve(container, ref container.Comp, logIfMissing))
            return false;
        if (!TryGetSolution(container, solutionId, out var solution, logIfMissing))
            return false;
        foundSolution = solution;
        return true;
    }

    public bool ResolveSolution(Entity<SolutionHolderComponent?> container,
        string solutionId,
        [NotNullWhen(true)] ref Entity<SolutionComponent>? foundSolution,
        bool logIfMissing = true)
    {
        if (!Resolve(container, ref container.Comp, logIfMissing))
            return false;
        if (!TryGetSolution(container, solutionId, out var solution, logIfMissing))
            return false;
        foundSolution = solution;
        return true;
    }

    public bool ResolveSolution(Entity<SolutionHolderComponent?, ContainerManagerComponent?> container,
        string solutionId,
        [NotNullWhen(true)] ref Entity<SolutionComponent>? solution)
    {
        if (!solution.HasValue
            || !TryEnsureSolution(container, solutionId, out var foundSolution))
            return false;
        solution = foundSolution;
        return true;
    }

    [PublicAPI]
    public bool TryEnsureSolution(Entity<SolutionHolderComponent?, ContainerManagerComponent?> container,
        string solutionId,
        out Entity<SolutionComponent> solution,
        float temperature = Atmospherics.T20C,
        bool canOverflow = true,
        bool canReact = true)
    {
        return TryEnsureSolution(container, solutionId, out solution, FixedPoint2.MaxValue, temperature, canOverflow, canReact);
    }


    public bool RemoveSolution(Entity<SolutionHolderComponent?, ContainerManagerComponent?> container,
        string solutionId)
    {
        if (!HolderQuery.Resolve(container, ref container.Comp1)
            || ContainerManQuery.Resolve(container, ref container.Comp2))
            return false;
        for (var i = 0; i < container.Comp1.SolutionIds.Count; i++)
        {
            var solId = container.Comp1.SolutionIds[i];
            if (solId != solutionId)
                continue;
            var solution = container.Comp1.Solutions[i];
            ContainerSystem.RemoveEntity(container, solution, container.Comp2, force: true);
            return true;
        }
        return false;
    }

    public bool RemoveSolution(Entity<SolutionComponent?> solution)
    {
        if (!Resolve(solution, ref solution.Comp)
            || solution.Comp.Parent == EntityUid.Invalid)
            return false;
        return RemoveSolution((solution.Comp.Container, solution.Comp.Container, null),
            solution.Comp.Name);
    }



    /// <summary>
    /// Ensures that the specified entity will have a solution with the specified id, creating a solution if not already present.
    /// This will return false on clients if the solution is not found!
    /// </summary>
    /// <param name="container">Entity that "contains" the solution</param>
    /// <param name="solutionId">Unique Identifier for the solution</param>
    /// <param name="solution">Solution</param>
    /// <param name="maxVolume"></param>
    /// <param name="temperature"></param>
    /// <param name="canOverflow"></param>
    /// <param name="canReact"></param>
    /// <returns>True if successful, False if there was an error or if a solution is not found on the client</returns>
    [PublicAPI]
    public bool TryEnsureSolution(Entity<SolutionHolderComponent?,ContainerManagerComponent?> container,
        string solutionId,
        out Entity<SolutionComponent> solution,
        FixedPoint2 maxVolume,
        float temperature = Atmospherics.T20C,
        bool canOverflow = true,
        bool canReact = true)
    {
        if (!Resolve(container, ref container.Comp1, false))
            AddComp<SolutionHolderComponent>(container);
        var solutionContainer = ContainerSystem.EnsureContainer<ContainerSlot>(container,
            FormatSolutionContainerId(solutionId),
            container);
        if (solutionContainer.ContainedEntity != null)
        {
            if (!TryComp(solutionContainer.ContainedEntity, out SolutionComponent? oldSolComp))
            {
                throw new Exception($"Entity: {ToPrettyString(solutionContainer.ContainedEntity)} " +
                                    $"in container: {solutionContainer.ID} does not have a solution component! " +
                                    $"This should never happen!");
            }
            solution = (solutionContainer.ContainedEntity.Value, oldSolComp);
            return true;
        }
        //todo client/server overrides
        if (NetManager.IsClient)
        {
            solution = default;
            return false;
        }
        var solEnt = Spawn();

        var solComp = new SolutionComponent
        {
            CanOverflow = canOverflow,
            CanReact = canReact,
            MaxVolume = maxVolume,
            Temperature = temperature

        };
        AddComp(solEnt, solComp);
        if (!ContainerSystem.Insert(solEnt, solutionContainer))
        {
            Del(solEnt);
            solution = default;
            return false;
        }
        solution = (solEnt, solComp);
        return true;
    }

    public bool TryGetSolutionWithComp<TComp>(Entity<SolutionHolderComponent> solutionHolder,
        string solutionName,
        out Entity<SolutionComponent,TComp> foundSolution)
        where TComp: Component, new()
    {
        var query = EntityManager.GetEntityQuery<TComp>();
        foreach (var solution in solutionHolder.Comp.Solutions)
        {
            if (!query.TryComp(solution, out var comp))
                continue;
            if (solution.Comp.Name != solutionName)
                continue;
            foundSolution = (solution, solution, comp);
            return true;
        }
        foundSolution = default;
        return false;
    }

    public bool TryGetFirstSolution(Entity<SolutionHolderComponent?> solutionHolder, out Entity<SolutionComponent> solution)
    {
        if (!Resolve(solutionHolder, ref solutionHolder.Comp)
            || solutionHolder.Comp.Solutions.Count == 0
            )
        {
            solution = default;
            return false;
        }
        solution = solutionHolder.Comp.Solutions[0];
        return true;
    }

    public bool TryGetSolutionWithComp<TComp, TComp2>(Entity<SolutionHolderComponent> solutionHolder,
        string solutionName,
        out Entity<SolutionComponent, TComp, TComp2> foundSolution)
        where TComp : Component, new()
        where TComp2 : Component, new()
    {
        var query = EntityManager.GetEntityQuery<TComp>();
        var query2 = EntityManager.GetEntityQuery<TComp2>();
        foreach (var solution in solutionHolder.Comp.Solutions)
        {
            if (!query.TryComp(solution, out var comp)
                || !query2.TryComp(solution, out var comp2))
                continue;
            if (solution.Comp.Name != solutionName)
                continue;
            foundSolution = (solution, solution, comp, comp2);
            return true;
        }
        foundSolution = default;
        return false;
    }

    public bool TryGetSolutionWithComp<TComp, TComp2, TComp3>(Entity<SolutionHolderComponent> solutionHolder,
        string solutionName,
        out Entity<SolutionComponent, TComp, TComp2, TComp3> foundSolution)
        where TComp : Component, new()
        where TComp2 : Component, new()
        where TComp3 : Component, new()
    {
        var query = EntityManager.GetEntityQuery<TComp>();
        var query2 = EntityManager.GetEntityQuery<TComp2>();
        var query3 = EntityManager.GetEntityQuery<TComp3>();
        foreach (var solution in solutionHolder.Comp.Solutions)
        {
            if (!query.TryComp(solution, out var comp)
                || !query2.TryComp(solution, out var comp2)
                || !query3.TryComp(solution, out var comp3))
                continue;
            if (solution.Comp.Name != solutionName)
                continue;
            foundSolution = (solution, solution, comp, comp2, comp3);
            return true;
        }
        foundSolution = default;
        return false;
    }


    public bool TryGetFirstSolutionWithComp<TComp>(Entity<SolutionHolderComponent> solutionHolder,
        out Entity<SolutionComponent,TComp> foundSolution)
        where TComp: Component, new()
    {
        var query = EntityManager.GetEntityQuery<TComp>();
        foreach (var solution in solutionHolder.Comp.Solutions)
        {
            if (!query.TryComp(solution, out var comp))
                continue;
            foundSolution = (solution, solution, comp);
            return true;
        }
        foundSolution = default;
        return false;
    }

    public bool TryGetFirstSolutionWithComp<TComp, TComp2>(Entity<SolutionHolderComponent> solutionHolder,
        out Entity<SolutionComponent, TComp, TComp2> foundSolution)
        where TComp : Component, new()
        where TComp2 : Component, new()
    {
        var query = EntityManager.GetEntityQuery<TComp>();
        var query2 = EntityManager.GetEntityQuery<TComp2>();
        foreach (var solution in solutionHolder.Comp.Solutions)
        {
            if (!query.TryComp(solution, out var comp)
                || !query2.TryComp(solution, out var comp2))
                continue;
            foundSolution = (solution, solution, comp, comp2);
            return true;
        }
        foundSolution = default;
        return false;
    }

    public bool TryGetFirstSolutionWithComp<TComp, TComp2, TComp3>(Entity<SolutionHolderComponent> solutionHolder,
        out Entity<SolutionComponent, TComp, TComp2, TComp3> foundSolution)
        where TComp : Component, new()
        where TComp2 : Component, new()
        where TComp3 : Component, new()
    {
        var query = EntityManager.GetEntityQuery<TComp>();
        var query2 = EntityManager.GetEntityQuery<TComp2>();
        var query3 = EntityManager.GetEntityQuery<TComp3>();
        foreach (var solution in solutionHolder.Comp.Solutions)
        {
            if (!query.TryComp(solution, out var comp)
                || !query2.TryComp(solution, out var comp2)
                || !query3.TryComp(solution, out var comp3))
                continue;
            foundSolution = (solution, solution, comp, comp2, comp3);
            return true;
        }
        foundSolution = default;
        return false;
    }


    public Entity<SolutionComponent, TComp>? GetSolutionWithComp<TComp>(Entity<SolutionHolderComponent> solutionHolder)
    where TComp: Component, new()
    {
        if (!TryGetFirstSolutionWithComp<TComp>(solutionHolder, out var data))
            return null;
        return data;
    }

    public Entity<SolutionComponent, TComp, TComp2>? GetSolutionWithComp<TComp,TComp2>(Entity<SolutionHolderComponent> solutionHolder)
        where TComp: Component, new()
        where TComp2: Component, new()
    {
        if (!TryGetFirstSolutionWithComp<TComp,TComp2>(solutionHolder, out var data))
            return null;
        return data;
    }

    public Entity<SolutionComponent, TComp, TComp2, TComp3>? GetSolutionWithComp<TComp,TComp2, TComp3>(Entity<SolutionHolderComponent> solutionHolder)
        where TComp: Component, new()
        where TComp2: Component, new()
        where TComp3: Component, new()
    {
        if (!TryGetFirstSolutionWithComp<TComp,TComp2,TComp3>(solutionHolder, out var data))
            return null;
        return data;
    }
    public IEnumerable<Entity<SolutionComponent, TComp>> GetSolutionsWithComp<TComp>(
        Entity<SolutionHolderComponent> solutionHolder) where TComp : Component, new()
    {
        var query = EntityManager.GetEntityQuery<TComp>();
        foreach (var solution in solutionHolder.Comp.Solutions)
        {
            if (query.TryComp(solution, out var comp))
                yield return (solution, solution, comp);
        }
    }

    public IEnumerable<Entity<SolutionComponent, TComp, TComp2>> GetSolutionsWithComps<TComp, TComp2>(
        Entity<SolutionHolderComponent> solutionHolder) where TComp : Component, new() where TComp2: Component, new()
    {
        var query1 = EntityManager.GetEntityQuery<TComp>();
        var query2 = EntityManager.GetEntityQuery<TComp2>();
        foreach (var solution in solutionHolder.Comp.Solutions)
        {
            if (query1.TryComp(solution, out var comp1) && query2.TryComp(solution, out var comp2))
                yield return (solution, solution, comp1, comp2);
        }
    }

    public IEnumerable<Entity<SolutionComponent, TComp, TComp2, TComp3>> GetSolutionsWithComps<TComp, TComp2, TComp3>(
        Entity<SolutionHolderComponent> solutionHolder)
        where TComp : Component, new()
        where TComp2: Component, new()
        where TComp3: Component, new()
    {
        var query1 = EntityManager.GetEntityQuery<TComp>();
        var query2 = EntityManager.GetEntityQuery<TComp2>();
        var query3 = EntityManager.GetEntityQuery<TComp3>();
        foreach (var solution in solutionHolder.Comp.Solutions)
        {
            if (query1.TryComp(solution, out var comp1)
                && query2.TryComp(solution, out var comp2)
                && query3.TryComp(solution, out var comp3))
                yield return (solution, solution, comp1, comp2, comp3);
        }
    }

    /// <summary>
    /// Formats a string as a solutionContainerId
    /// </summary>
    /// <param name="solutionId">SolutionId</param>
    /// <returns>Formated Container Id</returns>
    public string FormatSolutionContainerId(string solutionId) => $"{SolutionContainerPrefix}_{solutionId}";
}
