using System.Diagnostics.CodeAnalysis;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.Reagents;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.Systems;

public abstract class SharedChemistryRegistrySystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    private EntityQuery<ReactionDefinitionComponent> _reactionDefinitionQuery;
    private EntityQuery<ReagentDefinitionComponent> _reagentDefinitionQuery;

    public override void Initialize()
    {
        base.Initialize();

        _reactionDefinitionQuery = GetEntityQuery<ReactionDefinitionComponent>();
        _reagentDefinitionQuery = GetEntityQuery<ReagentDefinitionComponent>();

        _protoManager.PrototypesReloaded += ReloadData;
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

    public bool TryGetReagentById(string id, out Entity<ReagentDefinitionComponent> reagent)
    {
        reagent = default;
        if (!TryEnsureRegistry(out var registry))
            return false;

        if (!registry.Comp.Reagents.TryGetValue(id, out var reagentId) ||
            !_reagentDefinitionQuery.TryComp(reagentId, out var reagentComp))
        {
            return false;
        }

        reagent = (reagentId, reagentComp);
        return true;
    }

    public Entity<ReagentDefinitionComponent> Index(string id)
    {
        if (!TryIndex(id, out var result))
            throw new KeyNotFoundException($"{id} is not registered as a reagent!");
        return result;
    }

    public bool TryIndex(string id, out Entity<ReagentDefinitionComponent> reagentDefinition)
    {
        reagentDefinition = default;
        if (!TryEnsureRegistry(out var registry))
            return false;
        if (!registry.Comp.Reagents.TryGetValue(id, out var reagentId) ||
            !_reagentDefinitionQuery.TryComp(reagentId, out var reagentComp))
            return false;
        reagentDefinition = (reagentId, reagentComp);
        return true;
    }
}
