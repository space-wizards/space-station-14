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
                AdjustReagent(solution, reagentDef.Value, quantity, out var overflow, solutionData.Metadata);
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
            reagentData.UpdateReagentDef(ChemistryRegistry);
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
    /// <param name="reagentVariantData">Extra reagent data</param>
    /// <returns>If successful</returns>
    public bool TryAddReagent(Entity<SolutionComponent> solution,
        string reagentId,
        FixedPoint2 amount,
        out FixedPoint2 overflow,
        ReagentVariant? reagentVariantData = null)
    {
        overflow = 0;
        return ChemistryRegistry.TryIndex(reagentId, out var reagentDef)
               && TryAddReagent(solution, (reagentDef.Value, reagentDef.Value), amount, out overflow, reagentVariantData);
    }


    /// <summary>
    /// Try to change the quantity of reagent in a solution
    /// </summary>
    /// <param name="solution">The solution</param>
    /// <param name="reagentDef">Reagent definition</param>
    /// <param name="amount">Amount to change</param>
    /// <param name="overflow">Amount that over/underflows when trying to change</param>
    /// <param name="reagentVariantData">Extra reagent data</param>
    /// <returns>If successful</returns>
    public bool TryAddReagent(Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagentDef,
        FixedPoint2 amount,
        out FixedPoint2 overflow,
        ReagentVariant? reagentVariantData = null
        )
    {
        overflow = 0;
        return !AdjustReagent((solution, solution.Comp),
                   (reagentDef, reagentDef.Comp),
                   amount,
                   out overflow,
                   reagentVariantData);
    }


    protected bool TryGetReagentData(Span<ReagentQuantity> reagentContents,
        Entity<ReagentDefinitionComponent> reagentDef,
        [NotNullWhen(true)] out ReagentQuantity? quantity,
        out int index
        )
    {
        index = 0;
        quantity = null;
        foreach (ref var reagentData in reagentContents)
        {
            if (reagentData.ReagentId == reagentDef.Comp.Id)
            {
                quantity = reagentData;
            }
            index++;
        }
        return false;
    }

    protected bool TryGetReagentVariantData(Span<ReagentVariantQuantity> variantContents,
        Entity<ReagentDefinitionComponent> reagentDef,
        ReagentVariant reagentVariant,
        [NotNullWhen(true)] out ReagentVariantQuantity? quantity,
        out int index
    )
    {
        index = 0;
        quantity = null;
        foreach (ref var variantData in variantContents)
        {
            if (variantData.ReagentId == reagentDef.Comp.Id && reagentVariant.Equals(variantData.Variant))
            {
                quantity = variantData;
            }
            index++;
        }
        return false;
    }


    public int CountReagentVariantsOfType(Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagentDef,
        ReagentVariant? variant)
    {
        if (variant == null)
            return 0;
        var count = 0;
        foreach (ref var variantData in CollectionsMarshal.AsSpan(solution.Comp.VariantContents))
        {
            if (variantData.Variant == null
                || !variantData.Variant.Equals(variant)
                || variantData.ReagentId != reagentDef.Comp.Id)
                continue;

            count += Convert.ToInt32(variantData.Variant?.Equals(variant));
        }
        return count;
    }

    protected void AdjustAllReagentVariants(Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagentDef,
        FixedPoint2 delta,
        out FixedPoint2 overflow)
    {
        overflow = 0;
        int index = 0;
        Queue<int> indicesToRemove = new();
        foreach (ref var variantData in CollectionsMarshal.AsSpan(solution.Comp.VariantContents))
        {
            if (variantData.ReagentId != reagentDef.Comp.Id)
            {
                index++;
                continue;
            }

            switch (FixedPoint2.Sign(delta))
                {
                    case 0:
                        break;
                    case 1:
                    {
                        variantData.Quantity += delta;
                        if (solution.Comp.CanOverflow)
                        {
                            overflow = FixedPoint2.Max(0, solution.Comp.Volume + delta - solution.Comp.MaxVolume);
                            variantData.Quantity -= overflow;
                        }
                        break;
                    }
                    case 2:
                    {
                        variantData.Quantity += delta;
                        if (variantData.Quantity <= 0)
                        {
                            overflow = variantData.Quantity;
                            variantData.Quantity = 0;
                            indicesToRemove.Enqueue(index);
                        }
                        break;
                    }
                }
            delta -= overflow;
        }
        overflow = -delta;
        while (indicesToRemove.Count != 0)
        {
            solution.Comp.VariantContents.RemoveAt(indicesToRemove.Dequeue());
        }
    }

    protected bool AdjustReagentVariant(Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagentDef,
        FixedPoint2 delta,
        out FixedPoint2 overflow,
        ReagentVariant reagentVariant
    )
    {
        overflow = 0;
        var reagentVariantContents = CollectionsMarshal.AsSpan(solution.Comp.VariantContents);
        if (!TryGetReagentVariantData(reagentVariantContents, reagentDef, reagentVariant, out _, out var index))
        {
            delta = FixedPoint2.Max(0, delta);
            var newData = new ReagentVariantQuantity(reagentDef, reagentVariant, delta);
            solution.Comp.VariantContents.Add(newData);
            UpdateCachedData(solution);
            UpdateChemicals(solution);
            return true;
        }

        ref var variantData = ref reagentVariantContents[index];

        switch (FixedPoint2.Sign(delta))
        {
            case 0:
                return true;
            case 1:
            {
                variantData.Quantity += delta;
                if (solution.Comp.CanOverflow)
                {
                    overflow = FixedPoint2.Max(0, solution.Comp.Volume + delta - solution.Comp.MaxVolume);
                    variantData.Quantity -= overflow;
                }

                break;
            }
            case 2:
            {
                variantData.Quantity += delta;
                if (variantData.Quantity <= 0)
                {
                    overflow = variantData.Quantity;
                    variantData.Quantity = 0;
                    UpdateCachedData(solution);
                    UpdateChemicals(solution);
                    return true;
                }
                break;
            }
        }
        UpdateCachedData(solution);
        UpdateChemicals(solution);
        return true;
    }


    /// <summary>
    /// Change the amount of reagent in a solution, outputting the over/underflow if there is one.
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="reagentDef"></param>
    /// <param name="delta"></param>
    /// <param name="overflow"></param>
    /// <param name="reagentVariantData"></param>
    /// <returns>true if successful</returns>
    protected bool AdjustReagent(Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagentDef,
        FixedPoint2 delta,
        out FixedPoint2 overflow,
        ReagentVariant? reagentVariantData
        )
    {
        overflow = 0;
        if (reagentVariantData != null)
            return AdjustReagentVariant(solution, reagentDef, delta, out overflow, reagentVariantData);

        var splitToVariants= false;
        var variantCount = CountReagentVariantsOfType(solution, reagentDef, reagentVariantData);
        if (variantCount > 0)
        {
            var testDelta = delta / variantCount+1;
            //Check to make sure we don't get fucked by fixedpoint rounding when splitting.
            //If the volume is too small to split (for example trying to split a change of 0.01 between 2 variants),
            //It will just apply the change to the base quantity. Not ideal but doing a 'round robin' would be annoying
            if (testDelta != FixedPoint2.Zero)
            {
                splitToVariants = true;
                delta = testDelta;
            }
        }

        var contents = CollectionsMarshal.AsSpan(solution.Comp.Contents);
        if (!TryGetReagentData(contents, reagentDef, out _, out var index))
        {
            delta = FixedPoint2.Max(0, delta);
            var newData = new ReagentQuantity(reagentDef, delta);
            solution.Comp.Contents.Add(newData);
            UpdateCachedData(solution);
            UpdateChemicals(solution);
            return true;
        }

        ref var reagentData = ref contents[index];
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
                }
                break;
            }
        }
        if (splitToVariants)
            AdjustAllReagentVariants(solution, reagentDef, delta, out overflow);
        UpdateCachedData(solution);
        UpdateChemicals(solution);
        return true;
    }

    public void UpdateChemicals(Entity<SolutionComponent> solution,
        bool needsReactionsProcessing = true,
        ReactionMixerComponent? mixerComponent = null)
    {
        Resolve(solution.Comp.Parent, ref mixerComponent, false);
        if (needsReactionsProcessing && solution.Comp.CanReact)
            ChemicalReactionSystem.FullyReactSolution(solution, mixerComponent);
        UpdateCachedData(solution);

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
        var (uid, comp, appearanceComponent) = soln;
        var solution = comp.Solution;

        if (!EntityManager.EntityExists(uid) || !Resolve(uid, ref appearanceComponent, false))
            return;

        AppearanceSystem.SetData(uid, SolutionContainerVisuals.FillFraction, soln.Comp1.FillFraction, appearanceComponent);
        AppearanceSystem.SetData(uid, SolutionContainerVisuals.Color, GetSolutionColor((soln, soln)), appearanceComponent);

        if (solution.GetPrimaryReagentId() is { } reagent)
            AppearanceSystem.SetData(uid, SolutionContainerVisuals.BaseOverride, reagent.ToString(), appearanceComponent);
    }

    protected int GetReagentIndex(Entity<SolutionComponent> solution, string reagentId)
    {
        if (TryGetReagentIndex(solution, reagentId, out var index))
            return index;
        throw new KeyNotFoundException(
            $"Reagent with id:{reagentId} could not be found in solution:{ToPrettyString(solution)}");
    }

    protected bool TryGetReagentIndex(Entity<SolutionComponent> solution, string reagentId, out int index)
    {
        index = 0;
        foreach (ref var reagentData in CollectionsMarshal.AsSpan(solution.Comp.Contents))
        {
            if (reagentData.ReagentId == reagentId)
                return true;
            index++;
        }
        index = -1;
        return false;
    }

    public FixedPoint2 GetReagentQuantity(Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagentDefinition)
    {
        if (TryGetReagentQuantity(solution, reagentDefinition, out var quantity))
            return quantity;
        throw new KeyNotFoundException(
            $"Could not find reagent: {reagentDefinition.Comp.Id} in solution {ToPrettyString(solution)}");
    }

    public bool TryGetReagentQuantity(Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagentDefinition,
        out FixedPoint2 quantity)
    {
        quantity = 0;
        if (!TryGetReagentIndex(solution, reagentDefinition.Comp.Id, out var index))
            return false;
        quantity = solution.Comp.CachedTotalReagentVolumes[index];
        return true;
    }

    public bool TryGetReagentQuantity(Entity<SolutionComponent> solution,
        Entity<ReagentDefinitionComponent> reagentDefinition,
        out FixedPoint2 quantity,
        ReagentVariant variant
        )
    {
        quantity = 0;
        if (!TryGetReagentVariantData(CollectionsMarshal.AsSpan(solution.Comp.VariantContents), reagentDefinition, variant, out var found, out _))
            return false;
        quantity = found.Value.Quantity;
        return true;
    }

    /// <summary>
    /// Updates a solutions volume after it has been changed
    /// </summary>
    /// <param name="solution">Solution to update</param>
    protected void UpdateCachedData(Entity<SolutionComponent> solution)
    {
        FixedPoint2 newTotalVolume = 0;

        solution.Comp.CachedTotalReagentVolumes.Clear();
        var reagentContents = CollectionsMarshal.AsSpan(solution.Comp.Contents);
        foreach (ref var reagentData in reagentContents)
        {
            solution.Comp.CachedTotalReagentVolumes.Add(reagentData.Quantity);
            newTotalVolume += reagentData.Quantity;
        }

        foreach (ref var variantData in  CollectionsMarshal.AsSpan(solution.Comp.VariantContents))
        {
            if (!TryGetReagentIndex(solution, variantData.ReagentId, out var index))
            {
                if (!ChemistryRegistry.TryIndex(variantData.ReagentId, out var reagentDef))
                {
                    Log.Error($"Reagent {variantData.ReagentId} with associated variant data: {variantData} in solution: " +
                              $"{ToPrettyString(solution)} could not be found in reagent registry! ");
                    continue;
                }
                solution.Comp.Contents.Add(new ReagentQuantity(reagentDef.Value, 0));
                index = solution.Comp.Contents.Count - 1;
            }
            solution.Comp.CachedTotalReagentVolumes[index] += variantData.Quantity;
            newTotalVolume += variantData.Quantity;
        }

        foreach (ref var reagentData in CollectionsMarshal.AsSpan(solution.Comp.Contents))
        {
            newTotalVolume += reagentData.Quantity;
        }
        solution.Comp.Volume = newTotalVolume;
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

    public Color GetSolutionColor(Entity<SolutionComponent> solution,
        ICollection<Entity<ReagentDefinitionComponent>>? filterOut = null,
        bool invertFilter = false)
    {
        if (solution.Comp.Volume == 0)
            return Color.Transparent;
        List<(FixedPoint2 quant, Entity<ReagentDefinitionComponent>? def, string id)> blendData = new();
        var index = 0;
        var voidedVolume = solution.Comp.Volume;
        foreach (ref var volume in CollectionsMarshal.AsSpan(solution.Comp.CachedTotalReagentVolumes))
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
                        blendData.Add((volume, reagentData.ReagentDef, reagentData.ReagentId));
                        voidedVolume -= volume;
                        break;
                    }

                }
                else
                {
                    foreach (var filterEntry in filterOut)
                    {
                        if (filterEntry.Comp.Id != reagentData.ReagentId)
                            continue;
                        blendData.Add((volume, reagentData.ReagentDef, reagentData.ReagentId));
                        voidedVolume -= volume;
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
