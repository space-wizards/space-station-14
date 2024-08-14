using System.Diagnostics.CodeAnalysis;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.Containers;

namespace Content.Shared.Chemistry.Systems;

public partial class SharedSolutionSystem
{
    public bool TryGetSolution(Entity<SolutionContainerComponent?> containingEntity,
        string solutionId,
        out Entity<SolutionComponent> solution,
        bool logIfMissing = true)
    {
        if (!Resolve(containingEntity, ref containingEntity.Comp))
        {
            solution = default;
            if (logIfMissing)
            {
                Log.Error($"Target Entity: {ToPrettyString(containingEntity)} Does not have a SolutionContainerComponent " +
                          $"and does not have any solutions!");
            }
            return false;
        }
        for (var i = 0; i < containingEntity.Comp.SolutionIds.Count; i++)
        {
            var solEnt = containingEntity.Comp.SolutionEntities[i];
            if (containingEntity.Comp.SolutionIds[i] != solutionId)
                continue;
            if (!TryComp(solEnt, out SolutionComponent? solComp))
            {
                throw new Exception($"Solution Entity {ToPrettyString(containingEntity.Comp.SolutionEntities[i])}" +
                                    $" does not have SolutionComponent! This should never happen!");
            }
            solution = (solEnt, solComp);
            return true;
        }

        if (logIfMissing)
        {
            Log.Error($"Solution with ID: {solutionId}, could not be found in solution containing entity:" +
                      $" {ToPrettyString(containingEntity)}");
        }
        solution = default;
        return false;
    }

    public IEnumerable<Entity<SolutionComponent>> EnumerateSolutions(Entity<SolutionContainerComponent?> containingEntity)
    {
        if (!Resolve(containingEntity, ref containingEntity.Comp))
            yield break;
        for (var i = 0; i < containingEntity.Comp.SolutionIds.Count; i++)
        {
            var solEnt = containingEntity.Comp.SolutionEntities[i];
            if (!TryComp(solEnt, out SolutionComponent? solComp))
            {
                throw new Exception($"Entity:{solEnt} is in a solution container but does not have a solution component!");
            }
            yield return (solEnt, solComp);
        }
    }

    public bool ResolveSolution(Entity<SolutionContainerComponent?> containingEntity,
        string solutionId,
        [NotNullWhen(true)] ref Entity<SolutionComponent>? foundSolution,
        bool logIfMissing = true)
    {
        if (!Resolve(containingEntity, ref containingEntity.Comp, logIfMissing))
            return false;
        if (!TryGetSolution(containingEntity, solutionId, out var solution, logIfMissing))
            return false;
        foundSolution = solution;
        return true;
    }


    /// <summary>
    /// Ensures that the specified entity will have a solution with the specified id, creating a solution if not already present.
    /// This will return false on clients if the solution is not found!
    /// </summary>
    /// <param name="containingEntity">Entity that "contains" the solution</param>
    /// <param name="solutionId">Unique Identifier for the solution</param>
    /// <param name="solution">Solution</param>
    /// <returns>True if successful, False if there was an error or if a solution is not found on the client</returns>
    [PublicAPI]
    public bool TryEnsureSolution(Entity<SolutionContainerComponent?,ContainerManagerComponent?> containingEntity,
        string solutionId,
        out Entity<SolutionComponent> solution)
    {
        if (!Resolve(containingEntity, ref containingEntity.Comp1, false))
            AddComp<SolutionContainerComponent>(containingEntity);
        var solutionContainer = ContainerSystem.EnsureContainer<ContainerSlot>(containingEntity,
            FormatSolutionContainerId(solutionId),
            containingEntity);
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
        if (NetManager.IsClient)
        {
            solution = default;
            return false;
        }
        var solEnt = Spawn();
        var solComp = AddComp<SolutionComponent>(solEnt);
        if (!ContainerSystem.Insert(solEnt, solutionContainer))
        {
            Del(solEnt);
            solution = default;
            return false;
        }
        solution = (solEnt, solComp);
        return true;
    }
    /// <summary>
    /// Formats a string as a solutionContainerId
    /// </summary>
    /// <param name="solutionId">SolutionId</param>
    /// <returns>Formated Container Id</returns>
    public string FormatSolutionContainerId(string solutionId) => $"{SolutionContainerPrefix}_{solutionId}";



}
