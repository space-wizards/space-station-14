using Content.Server.Atmos.Components;
using Content.Server.Body.Components;
using Content.Server.Mind;
using Content.Server.Mind.Components;
using Content.Server.NPC;
using Content.Server.NPC.Components;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Server.Speech.Components;
using Content.Server.Temperature.Components;
using Content.Shared.Mobs;
using Content.Shared.Tag;

namespace Content.Server.Blob
{
    public sealed class ZombieBlobSystem : EntitySystem
    {
        [Dependency] private readonly NpcFactionSystem _faction = default!;
        [Dependency] private readonly NPCSystem _npc = default!;
        [Dependency] private readonly MindSystem _mind = default!;
        [Dependency] private readonly TagSystem _tagSystem = default!;

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
            component.OldFactions = oldFactions;

            var htn = EnsureComp<HTNComponent>(uid);
            htn.RootTask = "SimpleHostileCompound";
            htn.Blackboard.SetValue(NPCBlackboard.Owner, uid);

            var accent = EnsureComp<ReplacementAccentComponent>(uid);
            accent.Accent = "genericAggressive";

            _tagSystem.AddTag(uid, "BlobMob");

            EnsureComp<PressureImmunityComponent>(uid);

            EnsureComp<RespiratorImmunityComponent>(uid);

            if (TryComp<TemperatureComponent>(uid, out var temperatureComponent))
            {
                component.OldColdDamageThreshold = temperatureComponent.ColdDamageThreshold;
                temperatureComponent.ColdDamageThreshold = 0;
            }

            var mindComp = EnsureComp<MindContainerComponent>(uid);
            if (!_mind.TryGetMind(uid, out var mind, mindComp))
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

            if (HasComp<PressureImmunityComponent>(uid))
            {
                RemComp<PressureImmunityComponent>(uid);
            }

            if (HasComp<RespiratorImmunityComponent>(uid))
            {
                RemComp<RespiratorImmunityComponent>(uid);
            }

            if (TryComp<TemperatureComponent>(uid, out var temperatureComponent) && component.OldColdDamageThreshold != null)
            {
                temperatureComponent.ColdDamageThreshold = component.OldColdDamageThreshold.Value;
            }

            _tagSystem.RemoveTag(uid, "BlobMob");

            QueueDel(component.BlobPodUid);

            EnsureComp<NpcFactionMemberComponent>(uid);
            foreach (var factionId in component.OldFactions)
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
