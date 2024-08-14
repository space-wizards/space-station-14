using System.Linq;
using Content.Client.Chemistry.Containers.EntitySystems;
using Content.Shared.Atmos.Prototypes;
using Content.Shared.Body.Part;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Components.Solutions;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Systems;
using Content.Shared.Kitchen.Components;
using Content.Shared.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Client.Chemistry.EntitySystems;

/// <inheritdoc/>
public sealed class ChemistryGuideDataSystem : SharedChemistryGuideDataSystem
{
    [Dependency] private readonly ChemistryRegistrySystem _chemistryRegistry = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

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
        foreach (var reagent in _chemistryRegistry.EnumeratePrototypes())
        {
            _reagentSources.Add(reagent.ReagentDefinition.Id, new());
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

            if (!entProto.TryGetComponent<ExtractableComponent>(out var extractableComponent, _componentFactory))
                continue;

            //these bloat the hell out of blood/fat
            if (entProto.HasComponent<BodyPartComponent>())
                continue;

            //these feel obvious...
            if (entProto.HasComponent<PillComponent>())
                continue;

            if (!entProto.TryGetComponent<StartingSolutionsComponent>(out var startingSolutions, _componentFactory))
                continue;

            if (extractableComponent.JuiceSolution is { } juiceSolutionId
                && startingSolutions.Solutions.TryGetValue(juiceSolutionId, out var juiceSol)
                && juiceSol != null)
            {
                var data = new ReagentEntitySourceData(
                    new() { DefaultJuiceCategory },
                    entProto,
                    juiceSol.Value);
                foreach (var (id, _) in juiceSol.Value.Contents)
                {
                    _reagentSources[id].Add(data);
                }

                usedNames.Add(entProto.Name);
            }

            if (extractableComponent.GrindableSolution is { } grindableSolutionId
                && startingSolutions.Solutions.TryGetValue(grindableSolutionId, out var grindSol)
                && grindSol != null)
            {
                var data = new ReagentEntitySourceData(
                    new() { DefaultGrindCategory },
                    entProto,
                    grindSol.Value);
                foreach (var (id, _) in grindSol)
                {
                    _reagentSources[id].Add(data);
                }
                usedNames.Add(entProto.Name);
            }
        }
    }

    public List<ReagentSourceData> GetReagentSources(string id)
    {
        return _reagentSources.GetValueOrDefault(id) ?? new List<ReagentSourceData>();
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

    public readonly SolutionSpecifier Solution;

    public override int OutputCount => Solution.Contents.Count;

    public override string IdentifierString => SourceEntProto.Name;

    public ReagentEntitySourceData(List<ProtoId<MixingCategoryPrototype>> mixingType, EntityPrototype sourceEntProto,
        SolutionSpecifier solution)
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

