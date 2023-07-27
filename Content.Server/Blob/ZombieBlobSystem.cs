using Content.Server.Mind;
using Content.Server.Mind.Components;
using Content.Server.NPC;
using Content.Server.NPC.Components;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Server.Speech.Components;
using Content.Shared.Mobs;

namespace Content.Server.Blob
{
    public sealed class ZombieBlobSystem : EntitySystem
    {
        [Dependency] private readonly NpcFactionSystem _faction = default!;
        [Dependency] private readonly NPCSystem _npc = default!;
        [Dependency] private readonly MindSystem _mind = default!;

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

            var htn = EnsureComp<HTNComponent>(uid);
            htn.RootTask = "SimpleHostileCompound";
            htn.Blackboard.SetValue(NPCBlackboard.Owner, uid);

            var accent = EnsureComp<ReplacementAccentComponent>(uid);
            accent.Accent = "genericAggressive";

            var mindComp = EnsureComp<MindContainerComponent>(uid);
            if (_mind.TryGetMind(uid, out var mind, mindComp) && _mind.TryGetSession(mind, out var session))
            {
                _npc.WakeNPC(uid, htn);
            }
        }

        private void OnShutdown(EntityUid uid, ZombieBlobComponent component, ComponentShutdown args)
        {
            if (HasComp<BlobMobComponent>(uid))
            {
                RemComp<BlobMobComponent>(uid);
            }

            if (HasComp<HTNComponent>(uid))
            {
                RemComp<HTNComponent>(uid);
            }

            if (HasComp<ReplacementAccentComponent>(uid))
            {
                RemComp<ReplacementAccentComponent>(uid);
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
