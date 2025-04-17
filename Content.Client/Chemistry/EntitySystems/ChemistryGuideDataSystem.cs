using System.Linq;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Atmos.Prototypes;
using Content.Shared.Body.Part;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Kitchen.Components;
using Content.Shared.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Client.Chemistry.EntitySystems;

/// <inheritdoc/>
public sealed class ChemistryGuideDataSystem : SharedChemistryGuideDataSystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    [ValidatePrototypeId<MixingCategoryPrototype>]
    private const string DefaultMixingCategory = "DummyMix";
    [ValidatePrototypeId<MixingCategoryPrototype>]
    private const string DefaultGrindCategory = "DummyGrind";
    [ValidatePrototypeId<MixingCategoryPrototype>]
    private const string DefaultJuiceCategory = "DummyJuice";
    [ValidatePrototypeId<MixingCategoryPrototype>]
    private const string DefaultCondenseCategory = "DummyCondense";

    private readonly Dictionary<string, List<ReagentSourceData>> _reagentSources = new();

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ReagentGuideRegistryChangedEvent>(OnReceiveRegistryUpdate);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        OnPrototypesReloaded(null);
    }

    private void OnReceiveRegistryUpdate(ReagentGuideRegistryChangedEvent message)
    {
        var data = message.Changeset;
        foreach (var remove in data.Removed)
        {
            Registry.Remove(remove);
        }

        foreach (var (key, val) in data.GuideEntries)
        {
            Registry[key] = val;
        }
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs? ev)
    {
        // this doesn't check what prototypes are being reloaded because, to be frank, we use a lot of them.
        _reagentSources.Clear();
        foreach (var reagent in PrototypeManager.EnumeratePrototypes<ReagentPrototype>())
        {
            _reagentSources.Add(reagent.ID, new());
        }

        foreach (var reaction in PrototypeManager.EnumeratePrototypes<ReactionPrototype>())
        {
            if (!reaction.Source)
                continue;

            var data = new ReagentReactionSourceData(
                reaction.MixingCategories ?? new () { DefaultMixingCategory },
                reaction);
            foreach (var product in reaction.Products.Keys)
            {
                _reagentSources[product].Add(data);
            }
        }

        foreach (var gas in PrototypeManager.EnumeratePrototypes<GasPrototype>())
        {
            if (gas.Reagent == null)
                continue;

            var data = new ReagentGasSourceData(
                new () { DefaultCondenseCategory },
                gas);
            _reagentSources[gas.Reagent].Add(data);
        }

        // store the names of the entities used so we don't get repeats in the guide.
        var usedNames = new List<string>();
        foreach (var entProto in PrototypeManager.EnumeratePrototypes<EntityPrototype>())
        {
            if (entProto.Abstract || usedNames.Contains(entProto.Name))
                continue;

            if (!entProto.TryGetComponent<ExtractableComponent>(out var extractableComponent, EntityManager.ComponentFactory))
                continue;

            //these bloat the hell out of blood/fat
            if (entProto.HasComponent<BodyPartComponent>())
                continue;

            //these feel obvious...
            if (entProto.HasComponent<PillComponent>())
                continue;

            if (extractableComponent.JuiceSolution is { } juiceSolution)
            {
                var data = new ReagentEntitySourceData(
                    new() { DefaultJuiceCategory },
                    entProto,
                    juiceSolution);
                foreach (var (id, _) in juiceSolution.Contents)
                {
                    _reagentSources[id.Prototype].Add(data);
                }

                usedNames.Add(entProto.Name);
            }


            if (extractableComponent.GrindableSolution is { } grindableSolutionId &&
                entProto.TryGetComponent<SolutionContainerManagerComponent>(out var manager, EntityManager.ComponentFactory) &&
                _solutionContainer.TryGetSolution(manager, grindableSolutionId, out var grindableSolution))
            {
                var data = new ReagentEntitySourceData(
                    new() { DefaultGrindCategory },
                    entProto,
                    grindableSolution);
                foreach (var (id, _) in grindableSolution.Contents)
                {
                    _reagentSources[id.Prototype].Add(data);
                }
                usedNames.Add(entProto.Name);
            }
        }
    }

    public List<ReagentSourceData> GetReagentSources(string id)
    {
        return _reagentSources.GetValueOrDefault(id) ?? new List<ReagentSourceData>();
    }

    // Is handled on server and updated on client via ReagentGuideRegistryChangedEvent
    public override void ReloadAllReagentPrototypes()
    {
    }
}

/// <summary>
/// A generic class meant to hold information about a reagent source.
/// </summary>
public abstract class ReagentSourceData
{
    /// <summary>
    /// The mixing type that applies to this source.
    /// </summary>
    public readonly IReadOnlyList<ProtoId<MixingCategoryPrototype>> MixingType;

    /// <summary>
    /// The number of distinct outputs. Used for primary ordering.
    /// </summary>
    public abstract int OutputCount { get; }

    /// <summary>
    /// A text string corresponding to this source. Typically a name. Used for secondary ordering.
    /// </summary>
    public abstract string IdentifierString { get; }

    protected ReagentSourceData(List<ProtoId<MixingCategoryPrototype>> mixingType)
    {
        MixingType = mixingType;
    }
}

/// <summary>
/// Used to store a reagent source that's an entity with a corresponding solution.
/// </summary>
public sealed class ReagentEntitySourceData : ReagentSourceData
{
    public readonly EntityPrototype SourceEntProto;

    public readonly Solution Solution;

    public override int OutputCount => Solution.Contents.Count;

    public override string IdentifierString => SourceEntProto.Name;

    public ReagentEntitySourceData(List<ProtoId<MixingCategoryPrototype>> mixingType, EntityPrototype sourceEntProto, Solution solution)
        : base(mixingType)
    {
        SourceEntProto = sourceEntProto;
        Solution = solution;
    }
}

/// <summary>
/// Used to store a reagent source that comes from a reaction between multiple reagents.
/// </summary>
public sealed class ReagentReactionSourceData : ReagentSourceData
{
    public readonly ReactionPrototype ReactionPrototype;

    public override int OutputCount => ReactionPrototype.Products.Count + ReactionPrototype.Reactants.Count(r => r.Value.Catalyst);

    public override string IdentifierString => ReactionPrototype.ID;

    public ReagentReactionSourceData(List<ProtoId<MixingCategoryPrototype>> mixingType, ReactionPrototype reactionPrototype)
        : base(mixingType)
    {
        ReactionPrototype = reactionPrototype;
    }
}

/// <summary>
/// Used to store a reagent source that comes from gas condensation.
/// </summary>
public sealed class ReagentGasSourceData : ReagentSourceData
{
    public readonly GasPrototype GasPrototype;

    public override int OutputCount => 1;

    public override string IdentifierString => Loc.GetString(GasPrototype.Name);

    public ReagentGasSourceData(List<ProtoId<MixingCategoryPrototype>> mixingType, GasPrototype gasPrototype)
        : base(mixingType)
    {
        GasPrototype = gasPrototype;
    }
}

