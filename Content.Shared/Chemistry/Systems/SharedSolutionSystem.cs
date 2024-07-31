using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.Reagents;
using Content.Shared.Chemistry.Components.Solutions;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.Systems;

public abstract partial class SharedSolutionSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] protected readonly ChemicalReactionSystem ChemicalReactionSystem = default!;
    [Dependency] protected readonly ExamineSystemShared ExamineSystem = default!;
    [Dependency] protected readonly SharedAppearanceSystem AppearanceSystem = default!;
    [Dependency] protected readonly SharedHandsSystem Hands = default!;
    [Dependency] protected readonly SharedContainerSystem ContainerSystem = default!;
    [Dependency] protected readonly MetaDataSystem MetaDataSys = default!;
    [Dependency] protected readonly INetManager NetManager = default!;
    [Dependency] protected readonly SharedChemistryRegistrySystem ChemistryRegistry = default!;

    public const string SolutionContainerPrefix = "@Solution";

    //TODO: CVAR THESE!
    public const int ReagentAlloc = 2;
    public const int VariantAlloc = 1;

    public override void Initialize()
    {
        SubscribeLocalEvent<SolutionComponent, AfterAutoHandleStateEvent>(HandleSolutionState);
        SubscribeLocalEvent<SolutionComponent, MapInitEvent>(SolutionMapInit);
        SubscribeLocalEvent<InitialSolutionsComponent, MapInitEvent>(InitialSolutionMapInit);

    }

    private void SolutionMapInit(Entity<SolutionComponent> ent, ref MapInitEvent args)
    {
        TrimAllocs(ent);
    }

    private void InitialSolutionMapInit(Entity<InitialSolutionsComponent> ent, ref MapInitEvent args)
    {
        foreach (var (solutionId, initialReagents) in ent.Comp.Contents)
        {
            if (!EnsureSolution((ent, null), solutionId, out var solution))
            {
                throw new NotSupportedException($"Could not ensure solution with id:{solutionId} inside " +
                                                $"Entity:{ToPrettyString(ent)}");
            }
            if (initialReagents == null)
                continue;
            foreach (var (initialId, quantity) in initialReagents)
            {
                if (!ChemistryRegistry.TryIndex(initialId.ReagentId,
                        out var reagentDefinition,
                        true))
                    continue;
                AddReagent(solution, reagentDefinition.Value, quantity, initialId.Variant);
            }
        }
        RemCompDeferred(ent, ent.Comp);
    }

    private void HandleSolutionState(Entity<SolutionComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        foreach (ref var reagentData in CollectionsMarshal.AsSpan(ent.Comp.Contents))
        {
            reagentData.UpdateDef(ChemistryRegistry);
        }
    }

    /// <summary>
    /// Tries to get a solution contained in a specified entity
    /// </summary>
    /// <param name="containingEntity">Entity that "contains" the solution</param>
    /// <param name="solutionId">Unique solution identifier</param>
    /// <param name="foundSolution">The found solution</param>
    /// <returns>True if successful, False if not</returns>
    [PublicAPI]
    public bool TryGetSolution(Entity<ContainerManagerComponent?> containingEntity,
        string solutionId,
        out Entity<SolutionComponent> foundSolution)
    {
        if (!TryGetSolutionContainer(containingEntity, solutionId, out var solContainer)
            || solContainer.ContainedEntity == null
            || !TryComp<SolutionComponent>(solContainer.ContainedEntity, out var solComp)
            )
        {
            foundSolution = default;
            return false;
        }
        foundSolution = (solContainer.ContainedEntity.Value, solComp);
        return true;
    }

    /// <summary>
    /// Get a solution on a containing entity, throws if not present
    /// </summary>
    /// <param name="containingEntity">Entity that "contains" the solution</param>
    /// <param name="solutionId">Unique solution Identifier</param>
    /// <returns>Found Solution</returns>
    /// <exception cref="KeyNotFoundException">Solution with ID could not be found on the containing entity</exception>
    [PublicAPI]
    public Entity<SolutionComponent> GetSolution(Entity<ContainerManagerComponent?> containingEntity, string solutionId)
    {
        return !TryGetSolution(containingEntity, solutionId, out var solution)
            ? throw new KeyNotFoundException(
                $"{ToPrettyString(containingEntity)} Does not contain solution with ID: {solutionId}")
            : solution;
    }

    /// <summary>
    /// Ensures that the specified entity will have a solution with the specified id, creating a solution if not already present.
    /// This will return false on clients if the solution is not found!
    /// </summary>
    /// <param name="containingEntity">Entity that "contains" the solution</param>
    /// <param name="solutionId">Unique Identifier for the solution</param>
    /// <param name="solution">Solution</param>
    /// <returns>True if successful, False if not found (only happens on the client)</returns>
    [PublicAPI]
    public bool EnsureSolution(Entity<ContainerManagerComponent?> containingEntity,
        string solutionId,
        out Entity<SolutionComponent> solution)
    {
        ContainerSlot? solCont = null;
        if (NetManager.IsClient &&
            (!ContainerSystem.TryGetContainer(containingEntity,
                 GetContainerId(solutionId),
                 out var foundCont,
                 containingEntity.Comp)
             || (solCont = foundCont as ContainerSlot) == null)
           )
        {
            solution = default;
            return false;
        }

        solCont ??= ContainerSystem.EnsureContainer<ContainerSlot>(containingEntity,
            GetContainerId(solutionId),
            containingEntity.Comp);

        if (solCont.ContainedEntity == null)
        {
            var newEnt = Spawn();
            if (!ContainerSystem.Insert(newEnt, solCont))
            {
                Del(newEnt);
                solution = default;
                return false;
            }

            solution = (newEnt, AddComp<SolutionComponent>(newEnt));
            return true;
        }

        solution = (solCont.ContainedEntity.Value, Comp<SolutionComponent>(solCont.ContainedEntity.Value));
        return true;
    }

    /// <summary>
    /// Guarantee that a solution with the specified ID exists on the containing entity, creating a new one if not present.
    /// Warning this throws an exception if called on the client!
    /// </summary>
    /// <param name="containingEntity">Entity that "contains" the solution</param>
    /// <param name="solutionId">Unique Solution Identifier</param>
    /// <returns>The solution</returns>
    /// <exception cref="NotSupportedException">If called on the client</exception>
    /// <exception cref="Exception">If ensure solution fails for some reason</exception>
    [PublicAPI]
    public Entity<SolutionComponent> EnsureSolution(Entity<ContainerManagerComponent?> containingEntity,
        string solutionId)
    {
        if (NetManager.IsClient)
            throw new NotSupportedException("GuaranteeSolution is not supported on client!");
        if (!EnsureSolution(containingEntity, solutionId, out var foundSolution))
        {
            throw new Exception(
                $"Failed to ensure solution on entity: {ToPrettyString(containingEntity)} with solutionId: {solutionId}");
        }

        return foundSolution;
    }

    /// <summary>
    /// Formats a string as a solutionContainerId
    /// </summary>
    /// <param name="solutionId">SolutionId</param>
    /// <returns>Formated Container Id</returns>

    public string GetContainerId(string solutionId) => $"{SolutionContainerPrefix}_{solutionId}";

    protected bool TryGetSolutionContainer(Entity<ContainerManagerComponent?> container,
        string solutionId,
        [NotNullWhen(true)] out ContainerSlot? solutionContainer)
    {
        if (!Resolve(container, ref container.Comp))
        {
            solutionContainer = default;
            return false;
        }

        solutionContainer = default;
        return ContainerSystem.TryGetContainer(container, GetContainerId(solutionId), out var foundCont)
               && (solutionContainer = foundCont as ContainerSlot) == null;
    }
    public void UpdateChemicals(Entity<SolutionComponent> solution,
        bool needsReactionsProcessing = true,
        ReactionMixerComponent? mixerComponent = null)
    {
        Resolve(solution.Comp.Parent, ref mixerComponent, false);
        if (needsReactionsProcessing && solution.Comp.CanReact)
            ChemicalReactionSystem.FullyReactSolution(solution, mixerComponent);

        //TODO: move this to update volume!
        if (solution.Comp.CanOverflow && solution.Comp.OverflowAmount > FixedPoint2.Zero)
        {
            var overflowEv = new SolutionOverflowEvent(solution, solution.Comp.OverflowAmount);
            RaiseLocalEvent(solution, ref overflowEv);
        }

        UpdateAppearance((solution, solution, null));

        var changedEv = new SolutionChangedEvent(solution);
        RaiseLocalEvent(solution, ref changedEv);
    }

    public void UpdateAppearance(Entity<SolutionComponent, AppearanceComponent?> soln)
    {
        var (uid, solution, appearanceComponent) = soln;
        var solEnt = (uid, solution);
        if (!EntityManager.EntityExists(uid) || !Resolve(uid, ref appearanceComponent, false))
            return;

        AppearanceSystem.SetData(uid, SolutionContainerVisuals.FillFraction, solution.FillFraction, appearanceComponent);
        AppearanceSystem.SetData(uid, SolutionContainerVisuals.Color, GetSolutionColor((soln, soln)), appearanceComponent);

        if (TryGetPrimaryReagent(solEnt, out var reagent))
            AppearanceSystem.SetData(uid, SolutionContainerVisuals.BaseOverride, reagent.Value.ToString(), appearanceComponent);
    }
    public Color GetSolutionColor(Entity<SolutionComponent> solution,
        ICollection<Entity<ReagentDefinitionComponent>>? filterOut = null,
        bool invertFilter = false)
    {
        if (solution.Comp.Volume == 0)
            return Color.Transparent;
        List<(FixedPoint2 quant, Entity<ReagentDefinitionComponent>? def, string id)> blendData = new();
        var index = 0;
        var voidedVolume = solution.Comp.Volume;
        foreach (ref var quantData in CollectionsMarshal.AsSpan(solution.Comp.Contents))
        {
            var reagentData = solution.Comp.Contents[index];
            if (filterOut != null)
            {
                if (invertFilter)
                {
                    foreach (var filterEntry in filterOut)
                    {
                        if (filterEntry.Comp.Id == reagentData.ReagentId)
                            continue;
                        blendData.Add((quantData.Quantity, reagentData.ReagentDef, reagentData.ReagentId));
                        voidedVolume -= quantData.Quantity;
                        break;
                    }

                }
                else
                {
                    foreach (var filterEntry in filterOut)
                    {
                        if (filterEntry.Comp.Id != reagentData.ReagentId)
                            continue;
                        blendData.Add((quantData.Quantity, reagentData.ReagentDef, reagentData.ReagentId));
                        voidedVolume -= quantData.Quantity;
                        break;
                    }
                }
            }
            index++;
        }

        blendData.Sort((a, b)
            => a.Item1.CompareTo(b.Item1));

        Color mixColor = default;
        var totalVolume = solution.Comp.Volume - voidedVolume;

        bool firstEntry = false;
        foreach (var data in blendData)
        {
            var reagentDef = data.def;
            if (!ChemistryRegistry.ResolveReagent(data.id, ref reagentDef))
                continue;
            if (!firstEntry)
            {
                mixColor = reagentDef.Value.Comp.SubstanceColor;
                firstEntry = true;
            }
            var percentage = data.quant.Float() / totalVolume.Float();
            mixColor = Color.InterpolateBetween(mixColor, reagentDef.Value.Comp.SubstanceColor, percentage);
        }
        return !firstEntry ? Color.Transparent : mixColor;
    }

}
