using Content.Server.NPC.Components;
using Content.Server.NPC.Systems;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Robust.Shared.Map;

namespace Content.Server.Blob
{
    public sealed class ZombieBlobSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _map = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly NpcFactionSystem _faction = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ZombieBlobComponent, MobStateChangedEvent>(OnMobStateChanged);
            SubscribeLocalEvent<ZombieBlobComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<ZombieBlobComponent, ComponentShutdown>(OnShutdown);
        }

        private void OnStartup(EntityUid uid, ZombieBlobComponent component, ComponentStartup args)
        {
            EnsureComp<BlobMobComponent>(uid);

            var oldFactions = new List<string>();
            var factionComp = EnsureComp<NpcFactionMemberComponent>(uid);
            foreach (var factionId in new List<string>(factionComp.Factions))
            {
                oldFactions.Add(factionId);
                _faction.RemoveFaction(uid, factionId);
            }
            _faction.AddFaction(uid, "Blob");
            component.OldFations = oldFactions;
        }

        private void OnShutdown(EntityUid uid, ZombieBlobComponent component, ComponentShutdown args)
        {
            if (HasComp<BlobMobComponent>(uid))
            {
                RemComp<BlobMobComponent>(uid);
            }

            QueueDel(component.BlobPodUid);

            EnsureComp<NpcFactionMemberComponent>(uid);
            foreach (var factionId in component.OldFations)
            {
                _faction.AddFaction(uid, factionId);
            }
            _faction.RemoveFaction(uid, "Blob");
        }

        private void OnMobStateChanged(EntityUid uid, ZombieBlobComponent component, MobStateChangedEvent args)
        {
            if (args.NewMobState == MobState.Dead)
            {
                RemComp<ZombieBlobComponent>(uid);
            }
        }
    }
}
