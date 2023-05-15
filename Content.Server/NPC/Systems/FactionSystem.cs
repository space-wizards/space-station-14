using Content.Server.NPC.Components;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.NPC.Systems;

/// <summary>
///     Outlines faction relationships with each other.
/// </summary>
public sealed class FactionSystem : EntitySystem
{
    [Dependency] private readonly FactionExceptionSystem _factionException = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    private ISawmill _sawmill = default!;

    /// <summary>
    /// To avoid prototype mutability we store an intermediary data class that gets used instead.
    /// </summary>
    private Dictionary<string, FactionData> _factions = new();

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = Logger.GetSawmill("faction");
        SubscribeLocalEvent<FactionComponent, ComponentStartup>(OnFactionStartup);
        _protoManager.PrototypesReloaded += OnProtoReload;
        RefreshFactions();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _protoManager.PrototypesReloaded -= OnProtoReload;
    }

    private void OnProtoReload(PrototypesReloadedEventArgs obj)
    {
        if (!obj.ByType.ContainsKey(typeof(FactionPrototype)))
            return;

        RefreshFactions();
    }

    private void OnFactionStartup(EntityUid uid, FactionComponent component, ComponentStartup args)
    {
        RefreshFactions(component);
    }

    /// <summary>
    /// Refreshes the cached factions for this component.
    /// </summary>
    private void RefreshFactions(FactionComponent component)
    {
        component.FriendlyFactions.Clear();
        component.HostileFactions.Clear();

        foreach (var faction in component.Factions)
        {
            // YAML Linter already yells about this
            if (!_factions.TryGetValue(faction, out var factionData))
                continue;

            component.FriendlyFactions.UnionWith(factionData.Friendly);
            component.HostileFactions.UnionWith(factionData.Hostile);
        }
    }

    /// <summary>
    /// Adds this entity to the particular faction.
    /// </summary>
    public void AddFaction(EntityUid uid, string faction, bool dirty = true)
    {
        if (!_protoManager.HasIndex<FactionPrototype>(faction))
        {
            _sawmill.Error($"Unable to find faction {faction}");
            return;
        }

        var comp = EnsureComp<FactionComponent>(uid);
        if (!comp.Factions.Add(faction))
            return;

        if (dirty)
        {
            RefreshFactions(comp);
        }
    }

    /// <summary>
    /// Removes this entity from the particular faction.
    /// </summary>
    public void RemoveFaction(EntityUid uid, string faction, bool dirty = true)
    {
        if (!_protoManager.HasIndex<FactionPrototype>(faction))
        {
            _sawmill.Error($"Unable to find faction {faction}");
            return;
        }

        if (!TryComp<FactionComponent>(uid, out var component))
            return;

        if (!component.Factions.Remove(faction))
            return;

        if (dirty)
        {
            RefreshFactions(component);
        }
    }

    public IEnumerable<EntityUid> GetNearbyHostiles(EntityUid entity, float range, FactionComponent? component = null)
    {
        if (!Resolve(entity, ref component, false))
            return Array.Empty<EntityUid>();

        var hostiles = GetNearbyFactions(entity, range, component.HostileFactions);
        if (TryComp<FactionExceptionComponent>(entity, out var factionException))
        {
            // ignore anything from enemy faction that we are explicitly friendly towards
            return hostiles.Where(target => !_factionException.IsIgnored(factionException, target));
        }

        return hostiles;
    }

    public IEnumerable<EntityUid> GetNearbyFriendlies(EntityUid entity, float range, FactionComponent? component = null)
    {
        if (!Resolve(entity, ref component, false))
            return Array.Empty<EntityUid>();

        return GetNearbyFactions(entity, range, component.FriendlyFactions);
    }

    private IEnumerable<EntityUid> GetNearbyFactions(EntityUid entity, float range, HashSet<string> factions)
    {
        var xformQuery = GetEntityQuery<TransformComponent>();

        if (!xformQuery.TryGetComponent(entity, out var entityXform))
            yield break;

        foreach (var comp in _lookup.GetComponentsInRange<FactionComponent>(entityXform.MapPosition, range))
        {
            if (comp.Owner == entity)
                continue;

            if (!factions.Overlaps(comp.Factions))
                continue;

            yield return comp.Owner;
        }
    }

    public bool IsFriendly(EntityUid uidA, EntityUid uidB, FactionComponent? factionA = null, FactionComponent? factionB = null)
    {
        if (!Resolve(uidA, ref factionA, false) || !Resolve(uidB, ref factionB, false))
            return false;

        return factionA.Factions.Overlaps(factionB.Factions) || factionA.FriendlyFactions.Overlaps(factionB.Factions);
    }

    /// <summary>
    /// Makes the source faction friendly to the target faction, 1-way.
    /// </summary>
    public void MakeFriendly(string source, string target)
    {
        if (!_factions.TryGetValue(source, out var sourceFaction))
        {
            _sawmill.Error($"Unable to find faction {source}");
            return;
        }

        if (!_factions.ContainsKey(target))
        {
            _sawmill.Error($"Unable to find faction {target}");
            return;
        }

        sourceFaction.Friendly.Add(target);
        sourceFaction.Hostile.Remove(target);
        RefreshFactions();
    }

    private void RefreshFactions()
    {
        _factions.Clear();

        foreach (var faction in _protoManager.EnumeratePrototypes<FactionPrototype>())
        {
            _factions[faction.ID] = new FactionData()
            {
                Friendly = faction.Friendly.ToHashSet(),
                Hostile = faction.Hostile.ToHashSet(),
            };
        }

        foreach (var comp in EntityQuery<FactionComponent>(true))
        {
            comp.FriendlyFactions.Clear();
            comp.HostileFactions.Clear();
            RefreshFactions(comp);
        }
    }

    /// <summary>
    /// Makes the source faction hostile to the target faction, 1-way.
    /// </summary>
    public void MakeHostile(string source, string target)
    {
        if (!_factions.TryGetValue(source, out var sourceFaction))
        {
            _sawmill.Error($"Unable to find faction {source}");
            return;
        }

        if (!_factions.ContainsKey(target))
        {
            _sawmill.Error($"Unable to find faction {target}");
            return;
        }

        sourceFaction.Friendly.Remove(target);
        sourceFaction.Hostile.Add(target);
        RefreshFactions();
    }
}

