using Content.Server.NPC.Components;
using Robust.Shared.Prototypes;
using System.Linq;
using JetBrains.Annotations;

namespace Content.Server.NPC.Systems;

/// <summary>
///     Outlines faction relationships with each other.
/// </summary>
public sealed partial class NpcFactionSystem : EntitySystem
{
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
        SubscribeLocalEvent<NpcFactionMemberComponent, ComponentStartup>(OnFactionStartup);
        _protoManager.PrototypesReloaded += OnProtoReload;

        InitializeException();
        RefreshFactions();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _protoManager.PrototypesReloaded -= OnProtoReload;
    }

    private void OnProtoReload(PrototypesReloadedEventArgs obj)
    {
        if (!obj.ByType.ContainsKey(typeof(NpcFactionPrototype)))
            return;

        RefreshFactions();
    }

    private void OnFactionStartup(EntityUid uid, NpcFactionMemberComponent memberComponent, ComponentStartup args)
    {
        RefreshFactions(memberComponent);
    }

    /// <summary>
    /// Refreshes the cached factions for this component.
    /// </summary>
    private void RefreshFactions(NpcFactionMemberComponent memberComponent)
    {
        memberComponent.FriendlyFactions.Clear();
        memberComponent.HostileFactions.Clear();

        foreach (var faction in memberComponent.Factions)
        {
            // YAML Linter already yells about this
            if (!_factions.TryGetValue(faction, out var factionData))
                continue;

            memberComponent.FriendlyFactions.UnionWith(factionData.Friendly);
            memberComponent.HostileFactions.UnionWith(factionData.Hostile);
        }
    }

    /// <summary>
    /// Adds this entity to the particular faction.
    /// </summary>
    public void AddFaction(EntityUid uid, string faction, bool dirty = true)
    {
        if (!_protoManager.HasIndex<NpcFactionPrototype>(faction))
        {
            _sawmill.Error($"Unable to find faction {faction}");
            return;
        }

        var comp = EnsureComp<NpcFactionMemberComponent>(uid);
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
        if (!_protoManager.HasIndex<NpcFactionPrototype>(faction))
        {
            _sawmill.Error($"Unable to find faction {faction}");
            return;
        }

        if (!TryComp<NpcFactionMemberComponent>(uid, out var component))
            return;

        if (!component.Factions.Remove(faction))
            return;

        if (dirty)
        {
            RefreshFactions(component);
        }
    }

    /// <summary>
    /// Remove this entity from all factions.
    /// </summary>
    public void ClearFactions(EntityUid uid, bool dirty = true)
    {
        if (!TryComp<NpcFactionMemberComponent>(uid, out var component))
            return;

        component.Factions.Clear();

        if (dirty)
            RefreshFactions(component);
    }

    public IEnumerable<EntityUid> GetNearbyHostiles(EntityUid entity, float range, NpcFactionMemberComponent? component = null)
    {
        if (!Resolve(entity, ref component, false))
            return Array.Empty<EntityUid>();

        var hostiles = GetNearbyFactions(entity, range, component.HostileFactions);
        if (TryComp<FactionExceptionComponent>(entity, out var factionException))
        {
            // ignore anything from enemy faction that we are explicitly friendly towards
            return hostiles
                .Union(GetHostiles(entity, factionException))
                .Where(target => !IsIgnored(entity, target, factionException));
        }

        return hostiles;
    }

    [PublicAPI]
    public IEnumerable<EntityUid> GetNearbyFriendlies(EntityUid entity, float range, NpcFactionMemberComponent? component = null)
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

        foreach (var comp in _lookup.GetComponentsInRange<NpcFactionMemberComponent>(entityXform.MapPosition, range))
        {
            if (comp.Owner == entity)
                continue;

            if (!factions.Overlaps(comp.Factions))
                continue;

            yield return comp.Owner;
        }
    }

    public bool IsEntityFriendly(EntityUid uidA, EntityUid uidB, NpcFactionMemberComponent? factionA = null, NpcFactionMemberComponent? factionB = null)
    {
        if (!Resolve(uidA, ref factionA, false) || !Resolve(uidB, ref factionB, false))
            return false;

        return factionA.Factions.Overlaps(factionB.Factions) || factionA.FriendlyFactions.Overlaps(factionB.Factions);
    }

    public bool IsFactionFriendly(string target, string with)
    {
        return _factions[target].Friendly.Contains(with) && _factions[with].Friendly.Contains(target);
    }

    public bool IsFactionFriendly(string target, EntityUid with, NpcFactionMemberComponent? factionWith = null)
    {
        if (!Resolve(with, ref factionWith, false))
            return false;

        return factionWith.Factions.All(x => IsFactionFriendly(target, x)) ||
               factionWith.FriendlyFactions.Contains(target);
    }

    public bool IsFactionHostile(string target, string with)
    {
        return _factions[target].Hostile.Contains(with) && _factions[with].Hostile.Contains(target);
    }

    public bool IsFactionHostile(string target, EntityUid with, NpcFactionMemberComponent? factionWith = null)
    {
        if (!Resolve(with, ref factionWith, false))
            return false;

        return factionWith.Factions.All(x => IsFactionHostile(target, x)) ||
               factionWith.HostileFactions.Contains(target);
    }

    public bool IsFactionNeutral(string target, string with)
    {
        return !IsFactionFriendly(target, with) && !IsFactionHostile(target, with);
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

        foreach (var faction in _protoManager.EnumeratePrototypes<NpcFactionPrototype>())
        {
            _factions[faction.ID] = new FactionData()
            {
                Friendly = faction.Friendly.ToHashSet(),
                Hostile = faction.Hostile.ToHashSet(),
            };
        }

        foreach (var comp in EntityQuery<NpcFactionMemberComponent>(true))
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

