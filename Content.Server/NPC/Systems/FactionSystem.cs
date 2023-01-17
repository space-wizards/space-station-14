using System.Linq;
using Content.Server.NPC.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Clothing.Components;
using Content.Server.Store.Systems;
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

        /// <summary>
        /// To avoid prototype mutability we store an intermediary data class that gets used instead.
        /// </summary>
        private Dictionary<string, FactionData> _factions = new();

        public override void Initialize()
        {
            base.Initialize();
            _sawmill = Logger.GetSawmill("faction");
            SubscribeLocalEvent<FactionComponent, ComponentStartup>(OnFactionStartup);
            SubscribeLocalEvent<FactionComponent, GetNearbyHostilesEvent>(OnGetNearbyHostiles);
            SubscribeLocalEvent<FactionComponent, ItemPurchasedEvent>(OnPurchased);
            SubscribeLocalEvent<ClothingAddFactionComponent, GotEquippedEvent>(OnEquipped);
            SubscribeLocalEvent<ClothingAddFactionComponent, GotUnequippedEvent>(OnUnequipped);
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
            RefreshFactions();
        }

        private void OnFactionStartup(EntityUid uid, FactionComponent component, ComponentStartup args)
        {
            RefreshFactions(component);
        }

        private void OnGetNearbyHostiles(EntityUid uid, FactionComponent component, ref GetNearbyHostilesEvent args)
        {
            args.ExceptionalFriendlies.UnionWith(component.ExceptionalFriendlies);
        }

        /// <summary>
        /// If we bought something we probably don't want it to start biting us after it's automatically placed in our hands.
        /// If you do, consider finding a better solution to grenade penguin CBT.
        /// </summary>
        private void OnPurchased(EntityUid uid, FactionComponent component, ref ItemPurchasedEvent args)
        {
            component.ExceptionalFriendlies.Add(args.Purchaser);
        }

        private void OnEquipped(EntityUid uid, ClothingAddFactionComponent component, GotEquippedEvent args)
        {
            if (!TryComp<ClothingComponent>(uid, out var clothing))
                return;

            if (!clothing.Slots.HasFlag(args.SlotFlags))
                return;

            if (!TryComp<FactionComponent>(args.Equipee, out var factionComponent))
                return;

            if (factionComponent.Factions.Contains(component.Faction))
                return;

            component.IsActive = true;
            AddFaction(args.Equipee, component.Faction);
        }

        private void OnUnequipped(EntityUid uid, ClothingAddFactionComponent component, GotUnequippedEvent args)
        {
            if (!component.IsActive)
                return;

            component.IsActive = false;
            RemoveFaction(args.Equipee, component.Faction);
        }

        /// <summary>
        /// Refreshes the cached factions for this component.
        /// </summary>
        private void RefreshFactions(FactionComponent component)
        {
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

            var targets = new HashSet<EntityUid>();
            var eHostiles = new HashSet<EntityUid>();
            var eFriendlies = new HashSet<EntityUid>();

            foreach (var target in GetNearbyFactions(entity, range, component.HostileFactions))
            {
                targets.Add(target);
            }

            var ev = new GetNearbyHostilesEvent(eHostiles, eFriendlies);
            RaiseLocalEvent(entity, ref ev);

            targets.UnionWith(ev.ExceptionalHostiles);

            foreach (var friendly in ev.ExceptionalFriendlies)
            {
                targets.Remove(friendly);
            }

            return targets;
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

        public void AddFriendlyEntity(EntityUid uid, EntityUid fEntity, FactionComponent? component = null)
        {
            if (!Resolve(uid, ref component, false))
                return;

            component.ExceptionalFriendlies.Add(fEntity);
        }
    }

    /// <summary>
    /// Raised on an entity when it's trying to determine which nearby entities are hostile.
    /// </summary>
    /// <param name="ExceptionalHostiles">Entities that will be counted as hostile regardless of faction. Overriden by friendlies.</param>
    /// <param name="ExceptionalFriendlies">Entities that will be counted as friendly regardless of faction. Overrides hostiles. </param>
    [ByRefEvent]
    public readonly record struct GetNearbyHostilesEvent(HashSet<EntityUid> ExceptionalHostiles, HashSet<EntityUid> ExceptionalFriendlies);
}
