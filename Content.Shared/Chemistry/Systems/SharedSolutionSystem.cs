using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.Reagents;
using Content.Shared.Chemistry.Components.Solutions;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Types;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
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


    public override void Initialize()
    {
        SubscribeLocalEvent<SolutionComponent, AfterAutoHandleStateEvent>(HandleSolutionState);

        SubscribeLocalEvent<InitialSolutionsComponent, MapInitEvent>(InitialSolutionMapInit);

    }

    private void InitialSolutionMapInit(Entity<InitialSolutionsComponent> ent, ref MapInitEvent args)
    {
        foreach (var (solutionId, initialReagents) in ent.Comp.Contents)
        {
            if (!EnsureSolution((ent, null), solutionId, out var solution))
            {
                throw new NotSupportedException($"Could not ensure solution with id:{solutionId} inside Entity:{ToPrettyString(ent)}");
            }

            if (initialReagents == null)
                continue;
            foreach (var (solutionData, quantity) in initialReagents)
            {
                if (!ChemistryRegistry.TryIndex(solutionData.ReagentId, out var reagentDef))
                    continue;
                AdjustReagent(solution, reagentDef, quantity, out var overflow, solutionData.Metadata);
                if (overflow > 0)
                {
                    Log.Info($"{solutionId} inside entity: {ToPrettyString(ent)} overflowed on spawn. Is this intended?");
                }
            }
        }
    }

    private void HandleSolutionState(Entity<SolutionComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        foreach (ref var reagentData in CollectionsMarshal.AsSpan(ent.Comp.Contents))
        {
            reagentData.ReagentId.NetSync(EntityManager);
        }
    }


    /// <summary>
    /// Tries to get a solution contained in a specified entity
    /// </summary>
    /// <param name="containingEntity">Entity that "contains" the solution</param>
    /// <param name="solutionId">Unique solution identifier</param>
    /// <param name="foundSolution">The found solution</param>
    /// <returns>True if successful, False if not</returns>
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
    public Entity<SolutionComponent> GuaranteeSolution(Entity<ContainerManagerComponent?> containingEntity,
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
    /// Try to change the quantity of reagent in a solution
    /// </summary>
    /// <param name="solution">The solution</param>
    /// <param name="reagentId">Reagent id/name</param>
    /// <param name="amount">Amount to change</param>
    /// <param name="overflow">Amount that over/underflows when trying to change</param>
    /// <param name="reagentMetadata">Extra reagent data</param>
    /// <returns>If successful</returns>
    public bool TryAddReagent(Entity<SolutionComponent> solution,
        string reagentId,
        FixedPoint2 amount,
        out FixedPoint2 overflow,
        ReagentMetadata? reagentMetadata = null)
    {
        overflow = 0;
        return ChemistryRegistry.TryIndex(reagentId, out var reagentDef)
               && TryAddReagent(solution, (reagentDef, reagentDef), amount, out overflow, reagentMetadata);
    }


    /// <summary>
    /// Try to change the quantity of reagent in a solution
    /// </summary>
    /// <param name="solution">The solution</param>
    /// <param name="reagentDef">Reagent definition</param>
    /// <param name="amount">Amount to change</param>
    /// <param name="overflow">Amount that over/underflows when trying to change</param>
    /// <param name="reagentMetadata">Extra reagent data</param>
    /// <returns>If successful</returns>
    public bool TryAddReagent(Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagentDef,
        FixedPoint2 amount,
        out FixedPoint2 overflow,
        ReagentMetadata? reagentMetadata = null
        )
    {
        overflow = 0;
        return !AdjustReagent((solution, solution.Comp),
                   (reagentDef, reagentDef.Comp),
                   amount,
                   out overflow,
                   reagentMetadata);
    }


    /// <summary>
    /// Change the amount of reagent in a solution, outputting the over/underflow if there is one.
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="reagentDef"></param>
    /// <param name="delta"></param>
    /// <param name="overflow"></param>
    /// <param name="reagentMetadata"></param>
    /// <returns>true if successful</returns>
    protected bool AdjustReagent(Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagentDef,
        FixedPoint2 delta,
        out FixedPoint2 overflow,
        ReagentMetadata? reagentMetadata
        )
    {
        overflow = 0;
        var index = 0;
        foreach (ref var reagentData in CollectionsMarshal.AsSpan(solution.Comp.Contents))
        {
            if (reagentData.ReagentId.Id != reagentDef.Comp.Id)
            {
                index++;
                continue;
            }
            switch (FixedPoint2.Sign(delta))
            {
                case 0:
                    return true;
                case 1:
                {
                    reagentData.Quantity += delta;
                    if (solution.Comp.CanOverflow)
                    {
                        overflow = FixedPoint2.Max(0, solution.Comp.Volume + delta - solution.Comp.MaxVolume);
                        reagentData.Quantity -= overflow;
                    }
                    break;
                }
                case 2:
                {
                    reagentData.Quantity += delta;
                    if (reagentData.Quantity <= 0)
                    {
                        overflow = reagentData.Quantity;
                        reagentData.Quantity = 0;
                        if (reagentMetadata != null)
                            UpdateReagentMetadata(solution, reagentDef, delta - reagentData.Quantity, reagentMetadata);
                        solution.Comp.Contents.RemoveAt(index);
                        UpdateVolume(solution);
                        UpdateChemicals(solution);
                        return true;
                    }
                    break;
                }
            }
            if (reagentMetadata != null)
                UpdateReagentMetadata(solution, reagentDef, delta - reagentData.Quantity, reagentMetadata);
            UpdateVolume(solution);
            UpdateChemicals(solution);
            return true;
        }
        if (delta <= 0)
            return false;

        (List<ReagentMetadata> metadata,List<FixedPoint2> metadataVolumes)? newMetadata = null;
        if (reagentMetadata != null)
            newMetadata = ([reagentMetadata], [delta]);
        solution.Comp.Contents.Add(new ReagentQuantity(new ReagentDef(reagentDef, EntityManager), delta, newMetadata));
        UpdateVolume(solution);
        UpdateChemicals(solution);
        return true;
    }

    public void UpdateChemicals(Entity<SolutionComponent> solution,
        bool needsReactionsProcessing = true,
        ReactionMixerComponent? mixerComponent = null)
    {
        Resolve(solution.Comp.SolutionOwner, ref mixerComponent, false);
        if (needsReactionsProcessing && solution.Comp.CanReact)
            ChemicalReactionSystem.FullyReactSolution(solution, mixerComponent);
        //TODO - Jezi: Take into account reagent metadata volume
        UpdateVolume(solution);

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
        var (uid, comp, appearanceComponent) = soln;
        var solution = comp.Solution;

        if (!EntityManager.EntityExists(uid) || !Resolve(uid, ref appearanceComponent, false))
            return;

        AppearanceSystem.SetData(uid, SolutionContainerVisuals.FillFraction, solution.FillFraction, appearanceComponent);
        AppearanceSystem.SetData(uid, SolutionContainerVisuals.Color, solution.GetColor(PrototypeManager), appearanceComponent);

        if (solution.GetPrimaryReagentId() is { } reagent)
            AppearanceSystem.SetData(uid, SolutionContainerVisuals.BaseOverride, reagent.ToString(), appearanceComponent);
    }


    /// <summary>
    /// Updates the metadata on the reagent if it gets out of sync. If the specified metadata is not present it will be added.
    /// This should be called whenever any reagent quantity changes!
    /// </summary>
    /// <param name="solution">The solution</param>
    /// <param name="reagentDef">Reagent Definition</param>
    /// <param name="reagentDelta">Amount to change</param>
    /// <param name="metadata">Metadata to update/add</param>
    protected void UpdateReagentMetadata(Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagentDef,
        FixedPoint2 reagentDelta,
        ReagentMetadata metadata
        )
    {
        if (reagentDelta == 0)
            return;

        foreach (ref var reagentData in CollectionsMarshal.AsSpan(solution.Comp.Contents))
        {
            if (reagentData.ReagentId.Id != reagentDef.Comp.Id)
                continue;
            if (reagentData.Metadata == null)
            {
                reagentData.Metadata = new();
                reagentData.MetadataVolumes = new();
                if (FixedPoint2.Sign(reagentDelta) != 1)
                    return;
                reagentData.Metadata.Add(metadata);
                reagentData.MetadataVolumes.Add(reagentDelta);
            }

            var foundMetadata = CollectionsMarshal.AsSpan(reagentData.Metadata);
            var foundVolumes = CollectionsMarshal.AsSpan(reagentData.MetadataVolumes);
            int i;
            for (i = 0;i < reagentData.Metadata.Count; i++)
            {
                if (!foundMetadata[i].Equals(metadata))
                    continue;

                foundVolumes[i] += reagentDelta;

                if (foundVolumes[i] != 0)
                    return;
                reagentData.MetadataVolumes?.RemoveAt(i);
                return;
            }


        }

    }

    /// <summary>
    /// Updates a solutions volume after it has been changed
    /// </summary>
    /// <param name="solution">Solution to update</param>
    protected void UpdateVolume(Entity<SolutionComponent> solution)
    {
        FixedPoint2 newVolume = 0;
        foreach (ref var reagentData in CollectionsMarshal.AsSpan(solution.Comp.Contents))
        {
            newVolume += reagentData.Quantity;
        }
        solution.Comp.Volume = newVolume;
        Dirty(solution);
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
}
