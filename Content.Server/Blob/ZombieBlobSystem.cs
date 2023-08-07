using Content.Server.Atmos.Components;
using Content.Server.Body.Components;
using Content.Server.Chat.Managers;
using Content.Server.Mind;
using Content.Server.Mind.Components;
using Content.Server.NPC;
using Content.Server.NPC.Components;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Server.Speech.Components;
using Content.Server.Temperature.Components;
using Content.Shared.Mobs;
using Content.Shared.Physics;
using Content.Shared.Tag;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Systems;

namespace Content.Server.Blob
{
    public sealed class ZombieBlobSystem : EntitySystem
    {
        [Dependency] private readonly NpcFactionSystem _faction = default!;
        [Dependency] private readonly NPCSystem _npc = default!;
        [Dependency] private readonly MindSystem _mind = default!;
        [Dependency] private readonly TagSystem _tagSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly IChatManager _chatMan = default!;
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;
        [Dependency] private readonly FixtureSystem _fixtureSystem = default!;

        private const int ClimbingCollisionGroup = (int) (CollisionGroup.BlobImpassable);

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ZombieBlobComponent, MobStateChangedEvent>(OnMobStateChanged);
            SubscribeLocalEvent<ZombieBlobComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<ZombieBlobComponent, ComponentShutdown>(OnShutdown);
        }

        /// <summary>
        /// Replaces the current fixtures with non-climbing collidable versions so that climb end can be detected
        /// </summary>
        /// <returns>Returns whether adding the new fixtures was successful</returns>
        private void ReplaceFixtures(EntityUid uid, ZombieBlobComponent climbingComp, FixturesComponent fixturesComp)
        {
            foreach (var (name, fixture) in fixturesComp.Fixtures)
            {
                if (climbingComp.DisabledFixtureMasks.ContainsKey(name)
                    || fixture.Hard == false
                    || (fixture.CollisionMask & ClimbingCollisionGroup) == 0)
                    continue;

                climbingComp.DisabledFixtureMasks.Add(fixture.ID, fixture.CollisionMask & ClimbingCollisionGroup);
                _physics.SetCollisionMask(uid, fixture, fixture.CollisionMask & ~ClimbingCollisionGroup, fixturesComp);
            }
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

            if (TryComp<FixturesComponent>(uid, out var fixturesComp))
            {
                ReplaceFixtures(uid, component, fixturesComp);
            }

            var mindComp = EnsureComp<MindContainerComponent>(uid);
            if (_mind.TryGetMind(uid, out var mind, mindComp) && _mind.TryGetSession(mind, out var session))
            {
                _chatMan.DispatchServerMessage(session, Loc.GetString("blob-zombie-greeting"));

                _audio.PlayGlobal(component.GreetSoundNotification, session);
            }
            else
            {
                var htn = EnsureComp<HTNComponent>(uid);
                htn.RootTask = new HTNCompoundTask() {Task = "SimpleHostileCompound"};
                htn.Blackboard.SetValue(NPCBlackboard.Owner, uid);
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

            if (TryComp<FixturesComponent>(uid, out var fixtures))
            {
                foreach (var (name, fixtureMask) in component.DisabledFixtureMasks)
                {
                    if (!fixtures.Fixtures.TryGetValue(name, out var fixture))
                    {
                        continue;
                    }

                    _physics.SetCollisionMask(uid, fixture, fixture.CollisionMask | fixtureMask, fixtures);
                }
                component.DisabledFixtureMasks.Clear();
            }
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
