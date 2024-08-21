using System.Runtime.InteropServices;
using Content.Shared.Chemistry.Components.Reagents;
using Content.Shared.Chemistry.Components.Solutions;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

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
    [Dependency] protected readonly IRobustRandom Random = default!;
    [Dependency] protected readonly ReactiveSystem ReactiveSystem = default!;

    public const float TemperatureEpsilon = 0.0005f;
    public const string SolutionContainerPrefix = "@Solution_";
    public const string DefaultSolutionName = "Default";
    public const int SolutionAlloc = 2;
    public const int ReagentAlloc = 2;
    public const int VariantAlloc = 1;

    public EntityQuery<SolutionComponent> SolutionQuery;
    private EntityQuery<SolutionHolderComponent> _containerQuery;
    private EntityQuery<StandoutReagentComponent> _standoutReagentQuery;

    public override void Initialize()
    {
        SolutionQuery = EntityManager.GetEntityQuery<SolutionComponent>();
        _containerQuery = EntityManager.GetEntityQuery<SolutionHolderComponent>();

        SubscribeLocalEvent<SolutionComponent, AfterAutoHandleStateEvent>(HandleSolutionState);
        SubscribeLocalEvent<SolutionHolderComponent, AfterAutoHandleStateEvent>(HandleSolutionHolderState);
        SubscribeLocalEvent<SolutionComponent, MapInitEvent>(SolutionMapInit);
        SubscribeLocalEvent<StartingSolutionsComponent, MapInitEvent>(InitialSolutionMapInit);

    }

    private void HandleSolutionHolderState(Entity<SolutionHolderComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        ent.Comp.Solutions.Clear();
        foreach (var solEnt in ent.Comp.SolutionEntities)
        {
            if (!SolutionQuery.TryComp(solEnt, out var solComp))
            {
                Log.Error($"Entity: {ToPrettyString(solEnt)} is contained in a solution " +
                          $"but does not have a solution component!");
                continue;
            }
            ent.Comp.Solutions.Add((solEnt, solComp));
        }
    }

    private void SolutionMapInit(Entity<SolutionComponent> ent, ref MapInitEvent args)
    {
        TrimAllocs(ent);
    }

    private void InitialSolutionMapInit(Entity<StartingSolutionsComponent> ent, ref MapInitEvent args)
    {
        if (NetManager.IsClient)
            return;
        foreach (var (solutionId, initialReagents) in ent.Comp.Solutions)
        {
            if (!TryEnsureSolution((ent, null), solutionId, out var solution))
            {
                throw new NotSupportedException($"Could not ensure solution with id:{solutionId} inside " +
                                                $"Entity:{ToPrettyString(ent)}");
            }
            if (initialReagents == null)
                continue;
            foreach (var (reagentSpecifier, quantity) in initialReagents)
            {
                if (!ChemistryRegistry.TryIndex(reagentSpecifier.Id,
                        out var reagentDefinition,
                        true))
                    continue;
                AddReagent(solution, (reagentDefinition.Value, quantity), out _);
            }
        }
        RemCompDeferred(ent, ent.Comp);
    }

    private void HandleSolutionState(Entity<SolutionComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        foreach (ref var reagentData in CollectionsMarshal.AsSpan(ent.Comp.Contents))
        {
            if (!ChemistryRegistry.TryIndex(reagentData.ReagentId, out var reagentDef, true))
            {
                reagentData.IsValid = false;
                continue;
            }
            reagentData.ReagentEnt = reagentDef.Value;
            reagentData.IsValid = true;
        }
    }

    public void SetMaxVolume(Entity<SolutionComponent> solution, FixedPoint2 capacity, bool stopOverflow = false)
    {
        if (solution.Comp.MaxVolume == capacity)
            return;
        if (stopOverflow && solution.Comp.MaxVolume < solution.Comp.Volume)
            capacity = solution.Comp.Volume;
        solution.Comp.MaxVolume = capacity;

        //TODO: overflow check!
        Dirty(solution);
    }

    public void UpdateChemicals(Entity<SolutionComponent> solution,
        bool needsReactionsProcessing = true,
        ReactionMixerComponent? mixerComponent = null)
    {
        Resolve(solution.Comp.Container, ref mixerComponent, false);
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
            AppearanceSystem.SetData(uid, SolutionContainerVisuals.BaseOverride, reagent.ToString(), appearanceComponent);
    }

    public Color GetSolutionColor(Entity<SolutionComponent> solution,
        bool invertFilter = false,
        params ReagentDef[] filteredReagents)
    {
        return GetSolutionColor(solution, 0f, invertFilter, filteredReagents);
    }

    public Color GetSolutionColor(Entity<SolutionComponent> solution,
        float standoutIncrease,
        bool invertFilter = false,
        params ReagentDef[] filteredReagents)
    {
        if (solution.Comp.Volume == 0)
            return Color.Transparent;
        List<(FixedPoint2 quant, Entity<ReagentDefinitionComponent> def)> blendData = new();
        List<(FixedPoint2 quant, Entity<ReagentDefinitionComponent, StandoutReagentComponent> def)> standoutReagents = new();
        FixedPoint2 standoutVolume = 0;
        var index = 0;
        var voidedVolume = solution.Comp.Volume;
        foreach (ref var quantData in CollectionsMarshal.AsSpan(solution.Comp.Contents))
        {
            var reagentData = solution.Comp.Contents[index];
            if (filteredReagents.Length > 0)
            {
                if (invertFilter)
                {
                    foreach (var filterEntry in filteredReagents)
                    {
                        if (filterEntry.Id == reagentData.ReagentId)
                            continue;
                        blendData.Add((quantData.Quantity, reagentData.ReagentEnt));
                        voidedVolume -= quantData.Quantity;
                        if (_standoutReagentQuery.TryComp(reagentData.ReagentEnt, out var standoutReagent))
                        {
                            standoutReagents.Add((quantData.Quantity,
                                (reagentData.ReagentEnt, reagentData.ReagentEnt, standoutReagent)));
                            standoutVolume += quantData.Quantity;
                        }
                        break;
                    }
                }
                else
                {
                    foreach (var filterEntry in filteredReagents)
                    {
                        if (filterEntry.Id != reagentData.ReagentId)
                            continue;
                        blendData.Add((quantData.Quantity, reagentData.ReagentEnt));
                        voidedVolume -= quantData.Quantity;
                        if (_standoutReagentQuery.TryComp(reagentData.ReagentEnt, out var standoutReagent))
                        {
                            standoutReagents.Add((quantData.Quantity,
                                (reagentData.ReagentEnt, reagentData.ReagentEnt, standoutReagent)));
                            standoutVolume += quantData.Quantity;
                        }
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

        if (blendData.Count == 0)
            return Color.Transparent;
        mixColor = blendData[0].def.Comp.SubstanceColor;
        for (var i2 = 1; i2 < blendData.Count; i2++)
        {
            var (quantity, def) = blendData[i2];
            var percentage = quantity.Float() / totalVolume.Float();
            mixColor = Color.InterpolateBetween(mixColor, def.Comp.SubstanceColor, percentage);
        }

        if (standoutReagents.Count <= 0 || !(standoutIncrease > 0))
            return mixColor;
        {
            var standoutColor = standoutReagents[0].def.Comp1.SubstanceColor;
            for (var i3 = 1; i3 < standoutReagents.Count; i3++)
            {
                var (quant, def) = standoutReagents[i3];
                standoutColor = Color.InterpolateBetween(standoutColor,
                    def.Comp1.SubstanceColor,
                    quant.Float() / standoutVolume.Float());
            }
            mixColor = Color.InterpolateBetween(mixColor, standoutColor, standoutIncrease);
        }
        return mixColor;
    }

    public Color GetSolutionColor(SolutionContents solutionContents,
        bool invertFilter = false,
        params ReagentDef[] filteredReagents)
    {
        return GetSolutionColor(solutionContents,
            0f,
            invertFilter,
            filteredReagents);
    }

    public Color GetSolutionColor(SolutionContents solutionContents,
        float standoutIncrease,
        bool invertFilter = false,
        params ReagentDef[] filteredReagents)
    {
        if (solutionContents.Volume == 0)
            return Color.Transparent;
        List<(FixedPoint2 quant, Entity<ReagentDefinitionComponent> def)> blendData = new();
        List<(FixedPoint2 quant, Entity<ReagentDefinitionComponent, StandoutReagentComponent> def)> standoutReagents = new();
        FixedPoint2 standoutVolume = 0;
        var voidedVolume = solutionContents.Volume;
        foreach (var reagentQuant in solutionContents)
        {
            if (!reagentQuant.IsValid)
            {
                voidedVolume -= reagentQuant.Quantity;
                Log.Error($"{reagentQuant.Id} has an invalid reagent definition!");
                continue;
            }
            if (filteredReagents.Length > 0)
            {
                if (invertFilter)
                {
                    foreach (var filterEntry in filteredReagents)
                    {
                        if (filterEntry.Id == reagentQuant.ReagentDef.Id)
                            continue;
                        blendData.Add((reagentQuant.Quantity, reagentQuant.Entity));
                        voidedVolume -= reagentQuant.Quantity;
                        if (_standoutReagentQuery.TryComp(reagentQuant.Entity, out var standoutReagent))
                        {
                            standoutReagents.Add((reagentQuant.Quantity,
                                (reagentQuant.Entity, reagentQuant.Entity, standoutReagent)));
                            standoutVolume += reagentQuant.Quantity;
                        }
                        break;
                    }
                }
                else
                {
                    foreach (var filterEntry in filteredReagents)
                    {
                        if (filterEntry.Id != reagentQuant.ReagentDef.Id)
                            continue;
                        blendData.Add((reagentQuant.Quantity, reagentQuant.Entity));
                        voidedVolume -= reagentQuant.Quantity;
                        if (_standoutReagentQuery.TryComp(reagentQuant.Entity, out var standoutReagent))
                        {
                            standoutReagents.Add((reagentQuant.Quantity,
                                (reagentQuant.Entity, reagentQuant.Entity, standoutReagent)));
                            standoutVolume += reagentQuant.Quantity;
                        }
                        break;
                    }
                }
            }
        }

        blendData.Sort((a, b)
            => a.Item1.CompareTo(b.Item1));

        Color mixColor = default;
        var totalVolume = solutionContents.Volume - voidedVolume;

        if (blendData.Count == 0)
            return Color.Transparent;
        mixColor = blendData[0].def.Comp.SubstanceColor;
        for (var i2 = 1; i2 < blendData.Count; i2++)
        {
            var (quantity, def) = blendData[i2];
            var percentage = quantity.Float() / totalVolume.Float();
            mixColor = Color.InterpolateBetween(mixColor, def.Comp.SubstanceColor, percentage);
        }

        if (standoutReagents.Count <= 0 || !(standoutIncrease > 0))
            return mixColor;
        {
            var standoutColor = standoutReagents[0].def.Comp1.SubstanceColor;
            for (var i3 = 1; i3 < standoutReagents.Count; i3++)
            {
                var (quant, def) = standoutReagents[i3];
                standoutColor = Color.InterpolateBetween(standoutColor,
                    def.Comp1.SubstanceColor,
                    quant.Float() / standoutVolume.Float());
            }
            mixColor = Color.InterpolateBetween(mixColor, standoutColor, standoutIncrease);
        }
        return mixColor;
    }

}
