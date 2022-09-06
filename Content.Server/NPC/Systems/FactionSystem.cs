using Content.Server.NPC.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.NPC.Systems
{
    /// <summary>
    ///     Outlines faction relationships with each other.
    /// </summary>
    public sealed class FactionSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _protoManager = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;

        private ISawmill _sawmill = default!;

        public override void Initialize()
        {
            base.Initialize();
            _sawmill = Logger.GetSawmill("faction");
            SubscribeLocalEvent<FactionComponent, ComponentStartup>(OnFactionStartup);
            _protoManager.PrototypesReloaded += OnProtoReload;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _protoManager.PrototypesReloaded -= OnProtoReload;
        }

        private void OnProtoReload(PrototypesReloadedEventArgs obj)
        {
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
            foreach (var faction in component.Factions)
            {
                // YAML Linter already yells about this
                if (!_protoManager.TryIndex<FactionPrototype>(faction, out var factionProto))
                    continue;

                component.FriendlyFactions.UnionWith(factionProto.Friendly);
                component.HostileFactions.UnionWith(factionProto.Hostile);
            }
        }

        /// <summary>
        /// Adds this entity to the particular faction.
        /// </summary>
        public void AddFaction(EntityUid uid, string faction, bool dirty = true)
        {
            if (!_protoManager.HasIndex<FactionPrototype>(faction))
            {
                _sawmill.Error($"Unable to find action {faction}");
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
                _sawmill.Error($"Unable to find action {faction}");
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

            return GetNearbyFactions(entity, range, component.HostileFactions);
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

        /// <summary>
        /// Makes the source faction friendly to the target faction, 1-way.
        /// </summary>
        public void MakeFriendly(string source, string target)
        {
            if (!_protoManager.TryIndex<FactionPrototype>(source, out var sourceFaction))
            {
                _sawmill.Error($"Unable to find faction {source}");
                return;
            }

            if (!_protoManager.HasIndex<FactionPrototype>(target))
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
            if (!_protoManager.TryIndex<FactionPrototype>(source, out var sourceFaction))
            {
                _sawmill.Error($"Unable to find faction {source}");
                return;
            }

            if (!_protoManager.HasIndex<FactionPrototype>(target))
            {
                _sawmill.Error($"Unable to find faction {target}");
                return;
            }

            sourceFaction.Friendly.Remove(target);
            sourceFaction.Hostile.Add(target);
            RefreshFactions();
        }
    }
}
