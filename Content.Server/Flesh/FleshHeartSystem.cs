using System.Linq;
using Content.Server.AlertLevel;
using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Climbing;
using Content.Server.Flesh.FleshGrowth;
using Content.Server.Humanoid;
using Content.Server.Mind.Components;
using Content.Server.Popups;
using Content.Server.RoundEnd;
using Content.Server.Station.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Damage;
using Content.Shared.Destructible;
using Content.Shared.Flesh;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Tag;
using Robust.Server.Containers;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Flesh
{
    public sealed class FleshHeartSystem : EntitySystem
    {
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly ContainerSystem _containerSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        [Dependency] private readonly IMapManager _map = default!;
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly TagSystem _tagSystem = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;
        [Dependency] private readonly BodySystem _body = default!;
        [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
        [Dependency] private readonly HumanoidAppearanceSystem _sharedHuApp = default!;
        [Dependency] private readonly IPrototypeManager _proto = default!;
        [Dependency] private readonly SharedAppearanceSystem _sharedAppearance = default!;
        [Dependency] private readonly StationSystem _stationSystem = default!;
        [Dependency] private readonly AlertLevelSystem _alertLevel = default!;
        [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;

        public enum HeartStates
        {
            Base,
            Active,
            Disable
        }

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<FleshHeartComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<FleshHeartComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<FleshHeartComponent, DestructionEventArgs>(OnDestruction);
            SubscribeLocalEvent<FleshHeartComponent, ClimbedOnEvent>(OnClimbedOn);
        }

        private void OnShutdown(EntityUid uid, FleshHeartComponent component, ComponentShutdown args)
        {
            component.AmbientAudioStream?.Stop();
        }

        private void OnDestruction(EntityUid uid, FleshHeartComponent component, DestructionEventArgs args)
        {
            component.AmbientAudioStream?.Stop();
            var stationUid = _stationSystem.GetOwningStation(uid);
            if (stationUid != null)
            {
                _alertLevel.SetLevel(stationUid.Value, component.AlertLevelOnDeactivate, true,
                    true, true);
            }
            var xform = Transform(uid);
            var coordinates = xform.Coordinates;
            foreach (var ent in component.BodyContainer.ContainedEntities.ToArray())
            {
                component.BodyContainer.Remove(ent, EntityManager, force: true);
                Transform(ent).Coordinates = coordinates;
                ent.RandomOffset(1f);
            }
            var fleshTilesQuery = EntityQueryEnumerator<SpreaderFleshComponent>();
            while (fleshTilesQuery.MoveNext(out var ent, out var comp))
            {
                QueueDel(ent);
            }
            var fleshWalls = new List<EntityUid>();
            var fleshWallsQuery = EntityQueryEnumerator<TagComponent>();
            while (fleshWallsQuery.MoveNext(out var ent, out var comp))
            {
                var isFleshWall = _tagSystem.HasAllTags(ent, "Wall", "Flesh");
                if (isFleshWall)
                {
                    fleshWalls.Add(ent);
                }
            }
            foreach(var ent in fleshWalls.ToArray())
            {
                _damageableSystem.TryChangeDamage(ent, component.DamageMobsIfHeartDestruct);
            }
            foreach (var mob in component.EdgeMobs.ToArray())
            {
                _damageableSystem.TryChangeDamage(mob, component.DamageMobsIfHeartDestruct);
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            var fleshHeartQuery = EntityQueryEnumerator<FleshHeartComponent>();
            while (fleshHeartQuery.MoveNext(out var ent, out var comp))
            {
                switch (comp.State)
                {
                    case HeartStates.Base:
                    {
                        comp.Accumulator += frameTime;

                        if (comp.Accumulator <= 1)
                            continue;
                        comp.Accumulator -= 1;
                        if (comp.BodyContainer.ContainedEntities.Count >= comp.BodyToFinalStage)
                        {
                            comp.State = HeartStates.Active;
                            var location = Transform(ent).MapPosition;

                            _chat.DispatchGlobalAnnouncement(
                                Loc.GetString("flesh-heart-activate-warning", ("location", location.Position)),
                                playSound: false, colorOverride: Color.Red);
                            // _audioSystem.PlayGlobal("/Audio/Misc/notice1.ogg", Filter.Broadcast(), true);
                            var stationUid = _stationSystem.GetOwningStation(ent);
                            if (stationUid != null)
                            {
                                _alertLevel.SetLevel(stationUid.Value, comp.AlertLevelOnActivate, false,
                                    false, true, true);
                            }
                            _audioSystem.PlayGlobal(
                                "/Audio/Announcements/flesh_heart_activate.ogg", Filter.Broadcast(), true,
                                AudioParams.Default);
                            SpawnFleshFloorOnOpenTiles(comp, Transform(ent), 1);
                            _roundEndSystem.CancelRoundEndCountdown(stationUid);
                            _audioSystem.PlayPvs(comp.TransformSound, ent, comp.TransformSound.Params);
                            comp.AmbientAudioStream = _audioSystem.PlayGlobal(
                                "/Audio/Ambience/Objects/flesh_heart.ogg", Filter.Broadcast(), true,
                                AudioParams.Default.WithLoop(true).WithVolume(-3f));
                            _appearance.SetData(ent, FleshHeartVisuals.State, FleshHeartStatus.Active);
                        }
                        break;
                    }
                    case HeartStates.Active:
                    {
                        comp.SpawnMobsAccumulator += frameTime;
                        comp.SpawnObjectsAccumulator += frameTime;
                        comp.FinalStageAccumulator += frameTime;
                        var xform = Transform(ent);
                        if (comp.SpawnMobsAccumulator >= comp.SpawnMobsFrequency)
                        {
                            comp.SpawnMobsAccumulator = 0;
                            SpawnMonstersOnOpenTiles(comp, xform, comp.SpawnMobsAmount, comp.SpawnMobsRadius);
                        }

                        if (comp.SpawnObjectsAccumulator >= comp.SpawnObjectsFrequency)
                        {
                            comp.SpawnObjectsAccumulator = 0;
                            // SpawnObjectsOnOpenTiles(comp, xform, comp.SpawnObjectsAmount, comp.SpawnObjectsRadius);
                        }

                        if (comp.FinalStageAccumulator >= comp.TimeLiveFinalHeartToWin)
                        {
                            comp.State = HeartStates.Disable;
                            RaiseLocalEvent(new FleshHeartFinalEvent()
                            {
                                OwningStation = xform.GridUid,
                            });
                        }
                        break;
                    }
                    case HeartStates.Disable:
                    {
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void OnStartup(EntityUid uid, FleshHeartComponent component, ComponentStartup args)
        {
            component.BodyContainer = _containerSystem.EnsureContainer<Container>(uid, "bodyContainer");
        }

        private void OnClimbedOn(EntityUid uid, FleshHeartComponent component, ClimbedOnEvent args)
        {
            if (!CanAbsorb(uid, args.Climber, component))
            {
                _popup.PopupEntity(Loc.GetString("flesh-heart-cant-absorb-targer"),
                    args.Instigator, PopupType.Large);
                return;
            }

            if (!TryComp<FixturesComponent>(args.Climber, out var fixturesComponent))
            {
                _popup.PopupEntity(Loc.GetString("flesh-heart-cant-absorb-targer"),
                    args.Instigator, PopupType.Large);
                return;
            }

            if (fixturesComponent.Fixtures["fix1"].Density <= 60)
            {
                _popup.PopupEntity(
                    Loc.GetString("flesh-heart-cant-absorb-targer"),
                    uid, PopupType.Large);
                return;
            }

            var xform = Transform(args.Climber);

            if (TryComp(args.Climber, out ContainerManagerComponent? container))
            {
                foreach (var cont in container.GetAllContainers().ToArray())
                {
                    foreach (var ent in cont.ContainedEntities.ToArray())
                    {
                        {
                            if (HasComp<BodyPartComponent>(ent))
                            {
                                continue;
                            }
                            cont.Remove(ent, EntityManager, force: true);
                            Transform(ent).Coordinates = xform.Coordinates;
                            ent.RandomOffset(0.25f);
                        }
                    }
                }
            }

            if (TryComp<HumanoidAppearanceComponent>(args.Climber, out var HuAppComponent))
            {
                if (TryComp<BodyComponent>(args.Climber, out var bodyComponent))
                {
                    var parts = _body.GetBodyChildren(args.Climber, bodyComponent).ToArray();

                    foreach (var part in parts)
                    {
                        if (part.Component.PartType == BodyPartType.Head)
                            continue;

                        if (part.Component.PartType == BodyPartType.Torso)
                        {
                            foreach (var organ in _body.GetPartOrgans(part.Id, part.Component))
                            {
                                _body.DeleteOrgan(organ.Id);
                            }
                        }
                        else
                        {
                            _body.DeletePart(part.Id);
                        }
                    }
                }

                _bloodstreamSystem.TryModifyBloodLevel(args.Climber, -300);

                var skeletonSprites = _proto.Index<HumanoidSpeciesBaseSpritesPrototype>("MobSkeletonSprites");
                foreach (var (key, id) in skeletonSprites.Sprites)
                {
                    if (key != HumanoidVisualLayers.Head)
                    {
                        _sharedHuApp.SetBaseLayerId(args.Climber, key, id, humanoid: HuAppComponent);
                    }
                }

                _physics.SetDensity(args.Climber, fixturesComponent.Fixtures["fix1"], 50);

                if (TryComp<AppearanceComponent>(args.Climber, out var appComponent))
                {
                    _sharedAppearance.SetData(args.Climber, DamageVisualizerKeys.Disabled, true, appComponent);
                    _damageableSystem.TryChangeDamage(args.Climber,
                        new DamageSpecifier() { DamageDict = { { "Slash", 100 } } });
                }

                component.BodyContainer.Insert(args.Climber);
                _audioSystem.PlayPvs(component.TransformSound, uid, component.TransformSound.Params);
            }
        }

        private bool CanAbsorb(EntityUid uid, EntityUid dragged, FleshHeartComponent component)
        {
            if (!TryComp<MobStateComponent>(dragged, out var stateComponent))
                return false;

            if (stateComponent.CurrentState != MobState.Dead)
                return false;

            if (!Transform(uid).Anchored)
                return false;

            if (!TryComp<HumanoidAppearanceComponent>(dragged, out var humanoidAppearance))
                return false;

            if (!(component.SpeciesWhitelist.Contains(humanoidAppearance.Species)))
                return false;

            return !TryComp<MindComponent>(dragged, out var mindComp) || true;
        }

        private void SpawnObjectsOnOpenTiles(FleshHeartComponent component, TransformComponent xform, int amount, float radius)
        {
            if (!_map.TryGetGrid(xform.GridUid, out var grid))
                return;

            var localpos = xform.Coordinates.Position;
            var tilerefs = grid.GetLocalTilesIntersecting(
                new Box2(localpos + (-radius, -radius), localpos + (radius, radius))).ToArray();
            foreach (var tileref in tilerefs)
            {
                var canSpawnBlocker = true;
                foreach (var ent in grid.GetAnchoredEntities(tileref.GridIndices).ToList())
                {
                    if (_tagSystem.HasAnyTag(ent, "FleshBlocker", "Wall", "Window"))
                    {
                        canSpawnBlocker = false;
                    }
                }
                if (canSpawnBlocker)
                {
                    if (_random.Prob(0.01f))
                    {
                        EntityManager.SpawnEntity(component.FleshBlockerId,
                            tileref.GridIndices.ToEntityCoordinates(xform.GridUid.Value, _map));
                    }
                }
            }
        }

        private void SpawnFleshFloorOnOpenTiles(FleshHeartComponent component, TransformComponent xform, float radius)
        {
            if (!_map.TryGetGrid(xform.GridUid, out var grid))
                return;

            var localpos = xform.Coordinates.Position;
            var tilerefs = grid.GetLocalTilesIntersecting(
                new Box2(localpos + (-radius, -radius), localpos + (radius, radius))).ToArray();
            foreach (var tileref in tilerefs)
            {
                var canSpawnFloor = true;
                foreach (var ent in grid.GetAnchoredEntities(tileref.GridIndices).ToList())
                {
                    if (_tagSystem.HasAnyTag(ent, "Wall", "Window", "Flesh"))
                        canSpawnFloor = false;
                }
                if (canSpawnFloor)
                {
                    EntityManager.SpawnEntity(component.FleshTileId,
                        tileref.GridIndices.ToEntityCoordinates(xform.GridUid.Value, _map));
                }
            }
        }

        private void SpawnMonstersOnOpenTiles(FleshHeartComponent component, TransformComponent xform, int amount, float radius)
        {
            if (!_map.TryGetGrid(xform.GridUid, out var grid))
                return;

            var localpos = xform.Coordinates.Position;
            var tilerefs = grid.GetLocalTilesIntersecting(
                new Box2(localpos + (-radius, -radius), localpos + (radius, radius))).ToArray();
            _random.Shuffle(tilerefs);
            var physQuery = GetEntityQuery<PhysicsComponent>();
            var amountCounter = 0;
            foreach (var tileref in tilerefs)
            {
                var valid = true;
                foreach (var ent in grid.GetAnchoredEntities(tileref.GridIndices))
                {
                    if (!physQuery.TryGetComponent(ent, out var body))
                        continue;
                    if (body.BodyType != BodyType.Static ||
                        !body.Hard ||
                        (body.CollisionLayer & (int) CollisionGroup.Impassable) == 0)
                        continue;
                    valid = false;
                    break;
                }
                if (!valid)
                    continue;
                amountCounter++;

                var randomMob = _random.Pick(component.Spawns);
                var mob = Spawn(randomMob, tileref.GridIndices.ToEntityCoordinates(xform.GridUid.Value, _map));
                component.EdgeMobs.Add(mob);
                if (amountCounter >= amount)
                    return;
            }
        }

        public sealed class FleshHeartFinalEvent : EntityEventArgs
        {
            public EntityUid? OwningStation;
        }
    }
}
