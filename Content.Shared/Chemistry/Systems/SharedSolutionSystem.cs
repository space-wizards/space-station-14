using System.Runtime.InteropServices;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.Reagents;
using Content.Shared.Chemistry.Components.Solutions;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction;
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

    public const string SolutionContainerPrefix = "@Solution";

    //TODO: CVAR THESE!
    public const int SolutionAlloc = 2;
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

    public void SetCapacity(Entity<SolutionComponent> solution, FixedPoint2 capacity, bool stopOverflow = false)
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
        ICollection<Entity<ReagentDefinitionComponent>>? filter = null,
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
            if (filter != null)
            {
                if (invertFilter)
                {
                    foreach (var filterEntry in filter)
                    {
                        if (filterEntry.Comp.Id == reagentData.ReagentId)
                            continue;
                        blendData.Add((quantData.Quantity, reagentData.ReagentEnt, reagentData.ReagentId));
                        voidedVolume -= quantData.Quantity;
                        break;
                    }

                }
                else
                {
                    foreach (var filterEntry in filter)
                    {
                        if (filterEntry.Comp.Id != reagentData.ReagentId)
                            continue;
                        blendData.Add((quantData.Quantity, reagentData.ReagentEnt, reagentData.ReagentId));
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
