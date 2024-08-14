using System.Diagnostics.CodeAnalysis;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.Reagents;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

using ReagentProtoData = (Content.Shared.Chemistry.Components.Reagents.ReagentDefinitionComponent ReagentDefinition,
    Content.Shared.Chemistry.Components.Reagents.ReagentMetamorphicSpriteComponent? MetamorphicSprite);

namespace Content.Shared.Chemistry.Systems;

public abstract class SharedChemistryRegistrySystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    private const float DefaultMolarMass = 18;

    private EntityQuery<ReactionDefinitionComponent> _reactionDefinitionQuery;
    private EntityQuery<ReagentDefinitionComponent> _reagentDefinitionQuery;

    public override void Initialize()
    {
        base.Initialize();

        _reactionDefinitionQuery = GetEntityQuery<ReactionDefinitionComponent>();
        _reagentDefinitionQuery = GetEntityQuery<ReagentDefinitionComponent>();

        _protoManager.PrototypesReloaded += ReloadData;
    }

    public
        IEnumerable<ReagentProtoData> EnumeratePrototypes()
    {
        foreach (var entProto in _protoManager.EnumeratePrototypes<EntityPrototype>())
        {
            if (!entProto.TryGetComponent(out ReagentDefinitionComponent? reagentDef, _componentFactory))
                continue;
            entProto.TryGetComponent(out ReagentMetamorphicSpriteComponent? metaSprite, _componentFactory);
            yield return (reagentDef, metaSprite);
        }

        foreach (var reagentProto in _protoManager.EnumeratePrototypes<ReagentPrototype>())
        {
            var reagentDef = CreateReagentDefFromLegacyProto(reagentProto);
            TryCreateReagentMetaSpriteFromLegacyProto(reagentProto, out var metaSprite);
            yield return (reagentDef, metaSprite);
        }
    }

    public bool TryIndexPrototype(string protoId, out ReagentProtoData protoData)
    {
        protoData = default;
        if (_protoManager.TryIndex<EntityPrototype>(protoId, out var entProto))
        {
            if (!entProto.TryGetComponent(out ReagentDefinitionComponent? reagentDef, _componentFactory))
                return false;
            entProto.TryGetComponent(out ReagentMetamorphicSpriteComponent? metaSpriteData, _componentFactory);
            protoData = (reagentDef, metaSpriteData);
            return true;
        }

        if (!_protoManager.TryIndex<ReagentPrototype>(protoId, out var legacyProto))
            return false;
        var reagentDefComp = CreateReagentDefFromLegacyProto(legacyProto);
        TryCreateReagentMetaSpriteFromLegacyProto(legacyProto, out var metaSprite);
        protoData = (reagentDefComp, metaSprite);

        return true;
    }

    private void ReloadData(PrototypesReloadedEventArgs args)
    {
        // TODO this will break all existing entity uid reagents on reload if this ever runs
        // Log.Info("Reloading Registry...");
        // ClearData();
        // LoadData();
    }

    protected Entity<ChemistryRegistryComponent> CreateRegistry()
    {
        var registryId = Spawn(null, MapCoordinates.Nullspace);
        var registryComp = EnsureComp<ChemistryRegistryComponent>(registryId);
        return (registryId, registryComp);
    }

    public bool TryEnsureRegistry(out Entity<ChemistryRegistryComponent> registry)
    {
        registry = default;

        var query = EntityQueryEnumerator<ChemistryRegistryComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            registry = (uid, comp);
            return true;
        }

        if (_net.IsClient)
            return false;

        registry = CreateRegistry();
        return true;
    }

    public IEnumerable<Entity<ReactionDefinitionComponent>> EnumerateReactions()
    {
        if (!TryEnsureRegistry(out var registry))
            yield break;

        foreach (var reaction in registry.Comp.Reactions.Values)
        {
            if (_reactionDefinitionQuery.TryComp(reaction, out var comp))
                yield return (reaction, comp);
        }
    }

    public IEnumerable<Entity<ReagentDefinitionComponent>> EnumerateReagents()
    {
        if (!TryEnsureRegistry(out var registry))
            yield break;

        foreach (var reagent in registry.Comp.Reagents.Values)
        {
            if (_reagentDefinitionQuery.TryComp(reagent, out var comp))
                yield return (reagent, comp);
        }
    }

    public IEnumerable<ReagentDef> EnumerateReagentDefs()
    {
        foreach (var reagent in EnumerateReagents())
        {
            yield return new ReagentDef(reagent, null);
        }
    }

    public bool ResolveReagent(string id,
        [NotNullWhen(true)] ref Entity<ReagentDefinitionComponent>? reagent,
        bool logMissingRegistry = true)
    {
        if (reagent != null)
        {
            if (reagent.Value.Comp.Id != id)
                throw new KeyNotFoundException($"ReagentDefinition {ToPrettyString(reagent)} does not match Id:{id}!");
            return true;
        }

        if (TryIndex(id, out reagent))
            return true;
        if (logMissingRegistry)
            Log.Error($"reagent:{id} was not found in the reagent registry!");
        return false;
    }

    public ReagentDef GetReagentDef(string id, ReagentVariant? variantData = null)
    {
        if (!TryIndex(id, out var result))
            throw new KeyNotFoundException($"{id} is not registered as a reagent!");
        return new ReagentDef(result.Value, variantData);
    }

    public Entity<ReagentDefinitionComponent> GetReagentEntity(string id)
    {
        if (!TryIndex(id, out var result))
            throw new KeyNotFoundException($"{id} is not registered as a reagent!");
        return result.Value;
    }

    public bool TryGetReagentDef(string id,
        out ReagentDef reagent,
        ReagentVariant? variant,
        bool logMissing = false)
    {
        reagent = default;
        if (!TryGetReagentEntity(id, out var reagentDefEnt, logMissing))
            return false;
        reagent = new ReagentDef(reagentDefEnt.Value, variant);
        return true;
    }

    public bool TryGetReagentEntity(string id,
        [NotNullWhen(true)] out Entity<ReagentDefinitionComponent>? reagent,
        bool logMissing = false)
    {
        reagent = default;
        if (!TryEnsureRegistry(out var registry))
            return false;

        if (!registry.Comp.Reagents.TryGetValue(id, out var reagentId) ||
            !_reagentDefinitionQuery.TryComp(reagentId, out var reagentComp))
        {
            if (logMissing)
                Log.Error($"Reagent with id:{id} was not found in the registry!");
            return false;
        }

        reagent = (reagentId, reagentComp);
        return true;
    }

    public bool HasReagentDefinition(string id)
    {
        return TryEnsureRegistry(out var registry) && registry.Comp.Reagents.ContainsKey(id);
    }

    public ReagentDef IndexDef(string id, ReagentVariant? variant = null) => GetReagentDef(id, variant);

    public Entity<ReagentDefinitionComponent> Index(string id) => GetReagentEntity(id);

    public bool HasIndex(string id) => HasReagentDefinition(id);

    public bool TryIndex(string id,
        [NotNullWhen(true)] out Entity<ReagentDefinitionComponent>? reagentDefinition,
        bool logMissing = false) =>
        TryGetReagentEntity(id, out reagentDefinition, logMissing);

    protected bool TryCreateReagentMetaSpriteFromLegacyProto(ReagentPrototype proto,[NotNullWhen(true)] out ReagentMetamorphicSpriteComponent? comp)
    {
        if (proto.MetamorphicSprite == null)
        {
            comp = null;
            return false;
        }
        comp = new ReagentMetamorphicSpriteComponent
        {
            MetamorphicSprite = proto.MetamorphicSprite,
            MetamorphicMaxFillLevels = proto.MetamorphicMaxFillLevels,
            MetamorphicFillBaseName = proto.MetamorphicFillBaseName,
            MetamorphicChangeColor = proto.MetamorphicChangeColor,
        };
        return true;
    }

    protected ReagentDefinitionComponent CreateReagentDefFromLegacyProto(ReagentPrototype proto)
    {
        return new ReagentDefinitionComponent
        {
            Id = proto.ID,
            Group = proto.Group,
            NameLocId = proto.NameLocId,
            MolarMass = DefaultMolarMass,
            Recognizable = proto.Recognizable,
            PricePerUnit = proto.PricePerUnit,
            Flavor = proto.Flavor,
            DescriptionLocId = proto.DescriptionLocId,
            PhysicalDescriptionLocId = proto.PhysicalDescriptionLocId,
            FlavorMinimum = proto.FlavorMinimum,
            SubstanceColor = proto.SubstanceColor,
            SpecificHeat = proto.SpecificHeat,
            BoilingPoint = proto.BoilingPoint,
            MeltingPoint = proto.MeltingPoint,
            Slippery = proto.Slippery,
            Fizziness = proto.Fizziness,
            Viscosity = proto.Viscosity,
            FootstepSound = proto.FootstepSound,
            WorksOnTheDead = proto.WorksOnTheDead,
            Metabolisms = proto.Metabolisms,
            ReactiveEffects = proto.ReactiveEffects,
            TileReactions = proto.TileReactions,
            PlantMetabolisms = proto.PlantMetabolisms,
        };
    }
}
