using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.Reagents;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Systems;
using Content.Shared.Prototypes;
using Robust.Server.Containers;
using Robust.Server.GameStates;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.EntitySystems;

public sealed class ChemistryRegistrySystem : SharedChemistryRegistrySystem
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;

    private EntityQuery<ReactionDefinitionComponent> _reactionDefinitionQuery;
    private EntityQuery<ReagentDefinitionComponent> _reagentDefinitionQuery;

    public override void Initialize()
    {
        base.Initialize();

        _reactionDefinitionQuery = GetEntityQuery<ReactionDefinitionComponent>();
        _reagentDefinitionQuery = GetEntityQuery<ReagentDefinitionComponent>();

        SubscribeLocalEvent<ChemistryRegistryComponent, MapInitEvent>(OnRegistryMapInit);
        SubscribeLocalEvent<ChemistryRegistryComponent, EntInsertedIntoContainerMessage>(OnRegistryInserted);
        SubscribeLocalEvent<ChemistryRegistryComponent, EntRemovedFromContainerMessage>(OnRegistryRemoved);
    }

    private void OnRegistryMapInit(Entity<ChemistryRegistryComponent> ent, ref MapInitEvent args)
    {
        _pvsOverride.AddGlobalOverride(ent);
        _container.EnsureContainer<Container>(ent, ent.Comp.ContainerId);
        LoadData();
    }

    private void OnRegistryInserted(Entity<ChemistryRegistryComponent> registry, ref EntInsertedIntoContainerMessage args)
    {
        var inserted = args.Entity;
        if (_reactionDefinitionQuery.TryComp(inserted, out var reaction))
        {
            registry.Comp.Reactions[reaction.Id] = inserted;
            Dirty(registry);
        }

        if (_reagentDefinitionQuery.TryComp(inserted, out var reagent))
        {
            registry.Comp.Reagents[reagent.Id] = inserted;
            Dirty(registry);
        }
    }

    private void OnRegistryRemoved(Entity<ChemistryRegistryComponent> registry, ref EntRemovedFromContainerMessage args)
    {
        var removed = args.Entity;
        if (_reactionDefinitionQuery.TryComp(removed, out var reaction))
        {
            registry.Comp.Reactions.Remove(reaction.Id);
            Dirty(registry);
        }

        if (_reagentDefinitionQuery.TryComp(removed, out var reagent))
        {
            registry.Comp.Reagents.Remove(reagent.Id);
            Dirty(registry);
        }
    }

    private Entity<ChemistryRegistryComponent> EnsureRegistry()
    {
        return TryEnsureRegistry(out var registry)
            ? registry
            : CreateRegistry();
    }

    private void LoadData()
    {
        var registry = EnsureRegistry();

        Log.Info("Loading Legacy Prototypes...");
        ConvertLegacyReagentPrototypes(registry);
        ConvertLegacyReactionPrototypes(registry);
        Dirty(registry);
        Log.Info("Done");

        foreach (var proto in _prototypes.EnumeratePrototypes<EntityPrototype>())
        {
            if (LoadReagentData(registry, proto))
                continue;

            LoadReactionData(registry, proto);
        }

        Log.Info($"{registry.Comp.Reagents.Count} reagents registered");
        Log.Info($"{registry.Comp.Reactions.Count} reactions registered");
        Log.Info("Registry load complete!");
    }

    private bool LoadReagentData(Entity<ChemistryRegistryComponent> registry, EntityPrototype proto)
    {
        if (!proto.HasComponent<ReagentDefinitionComponent>(_compFactory))
            return false;

        registry.Comp.Reagents.Add(proto.ID, Spawn(proto.ID));
        return true;
    }

    private bool LoadReactionData(Entity<ChemistryRegistryComponent> registry, EntityPrototype proto)
    {
        if (!proto.HasComponent<ReactionDefinitionComponent>(_compFactory))
            return false;

        registry.Comp.Reactions.Add(proto.ID, Spawn(proto.ID));
        return true;
    }

    private void ConvertLegacyReagentPrototypes(Entity<ChemistryRegistryComponent> registry)
    {
        var container = _container.EnsureContainer<Container>(registry, registry.Comp.ContainerId);

        var i = 0;
        foreach (var reagentProto in _prototypes.EnumeratePrototypes<ReagentPrototype>())
        {
            var newEnt = Spawn();
            AddComp(newEnt, CreateReagentDefFromLegacyProto(reagentProto));
            if (TryCreateReagentMetaSpriteFromLegacyProto(reagentProto, out var metaSpriteComp))
                AddComp(newEnt, metaSpriteComp);
            _metaData.SetEntityName(newEnt, reagentProto.ID);
            _metaData.SetEntityDescription(newEnt, reagentProto.LocalizedDescription);
            _container.Insert(newEnt, container);
            i++;
        }

        Log.Info($"{i} legacy reagents loaded");
    }

    private void ConvertLegacyReactionPrototypes(Entity<ChemistryRegistryComponent> registry)
    {
        var container = _container.EnsureContainer<Container>(registry, registry.Comp.ContainerId);

        var i = 0;
        foreach (var reactionProto in _prototypes.EnumeratePrototypes<ReactionPrototype>())
        {
            var newEnt = Spawn();
            var reactionDef = AddComp<ReactionDefinitionComponent>(newEnt);
            var tempReq = AddComp<RequiresReactionTemperatureComponent>(newEnt);
            tempReq.MinimumTemperature = reactionProto.MinimumTemperature;
            tempReq.MaximumTemperature = reactionProto.MaximumTemperature;
            reactionDef.Id = reactionProto.ID;
            reactionDef.ConserveEnergy = reactionProto.ConserveEnergy;
            reactionDef.Effects = reactionProto.Effects;
            reactionDef.Impact = reactionProto.Impact;
            reactionDef.Sound = reactionProto.Sound;
            reactionDef.Quantized = reactionProto.Quantized;
            reactionDef.Priority = reactionProto.Priority;
            reactionDef.LegacyId = reactionProto.ID;

            reactionDef.Reactants = new();
            foreach (var (reagentId, data) in reactionProto.Reactants)
            {
                reactionDef.Reactants.Add(reagentId, new ReactantData(data.Amount, data.Catalyst));
            }

            if (reactionProto.MixingCategories != null)
            {
                var mixingReq = AddComp<RequiresReactionMixingComponent>(newEnt);
                mixingReq.MixingCategories = reactionProto.MixingCategories;
            }

            _metaData.SetEntityName(newEnt, reactionProto.Name);
            _container.Insert(newEnt, container);
            i++;
        }

        Log.Info($"{i} legacy reactions loaded");
    }
}
