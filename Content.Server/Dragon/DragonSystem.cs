using Content.Server.Body.Systems;
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.NPC;
using Content.Shared.Damage;
using Content.Shared.Dragon;
using Content.Shared.Examine;
using Content.Shared.Maps;
using Content.Shared.Movement.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Content.Server.NPC.Systems;
using Content.Server.Station.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Server.Dragon
{
    public sealed partial class DragonSystem : GameRuleSystem<DragonRuleComponent>
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDef = default!;
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
        [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        [Dependency] private readonly StationSystem _station = default!;
        [Dependency] private readonly NPCSystem _npc = default!;

        /// <summary>
        /// Minimum distance between 2 rifts allowed.
        /// </summary>
        private const int RiftRange = 15;

        /// <summary>
        /// Radius of tiles
        /// </summary>
        private const int RiftTileRadius = 2;

        private const int RiftsAllowed = 3;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DragonComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<DragonComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<DragonComponent, DragonDevourActionEvent>(OnDevourAction);
            SubscribeLocalEvent<DragonComponent, DragonSpawnRiftActionEvent>(OnDragonRift);
            SubscribeLocalEvent<DragonComponent, RefreshMovementSpeedModifiersEvent>(OnDragonMove);

            SubscribeLocalEvent<DragonComponent, DragonDevourDoAfterEvent>(OnDoAfter);

            SubscribeLocalEvent<DragonComponent, MobStateChangedEvent>(OnMobStateChanged);

            SubscribeLocalEvent<DragonRiftComponent, ComponentShutdown>(OnRiftShutdown);
            SubscribeLocalEvent<DragonRiftComponent, ComponentGetState>(OnRiftGetState);
            SubscribeLocalEvent<DragonRiftComponent, AnchorStateChangedEvent>(OnAnchorChange);
            SubscribeLocalEvent<DragonRiftComponent, ExaminedEvent>(OnRiftExamined);

            SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRiftRoundEnd);
        }

        private void OnDoAfter(EntityUid uid, DragonComponent component, DragonDevourDoAfterEvent args)
        {
            if (args.Handled || args.Cancelled)
                return;

            var ichorInjection = new Solution(component.DevourChem, component.DevourHealRate);

            //Humanoid devours allow dragon to get eggs, corpses included
            if (HasComp<HumanoidAppearanceComponent>(args.Args.Target))
            {
                ichorInjection.ScaleSolution(0.5f);
                component.DragonStomach.Insert(args.Args.Target.Value);
                _bloodstreamSystem.TryAddToChemicals(uid, ichorInjection);
            }

            //TODO: Figure out a better way of removing structures via devour that still entails standing still and waiting for a DoAfter. Somehow.
            //If it's not human, it must be a structure
            else if (args.Args.Target != null)
                EntityManager.QueueDeleteEntity(args.Args.Target.Value);

            _audioSystem.PlayPvs(component.SoundDevour, uid);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var comp in EntityQuery<DragonComponent>())
            {
                if (comp.WeakenedAccumulator > 0f)
                {
                    comp.WeakenedAccumulator -= frameTime;

                    // No longer weakened.
                    if (comp.WeakenedAccumulator < 0f)
                    {
                        comp.WeakenedAccumulator = 0f;
                        _movement.RefreshMovementSpeedModifiers(comp.Owner);
                    }
                }

                // At max rifts
                if (comp.Rifts.Count >= RiftsAllowed)
                {
                    continue;
                }

                // If there's an active rift don't accumulate.
                if (comp.Rifts.Count > 0)
                {
                    var lastRift = comp.Rifts[^1];

                    if (TryComp<DragonRiftComponent>(lastRift, out var rift) && rift.State != DragonRiftState.Finished)
                    {
                        comp.RiftAccumulator = 0f;
                        continue;
                    }
                }

                comp.RiftAccumulator += frameTime;

                // Delete it, naughty dragon!
                if (comp.RiftAccumulator >= comp.RiftMaxAccumulator)
                {
                    Roar(comp);
                    QueueDel(comp.Owner);
                }
            }

            foreach (var comp in EntityQuery<DragonRiftComponent>())
            {
                if (comp.State != DragonRiftState.Finished && comp.Accumulator >= comp.MaxAccumulator)
                {
                    // TODO: When we get autocall you can buff if the rift finishes / 3 rifts are up
                    // for now they just keep 3 rifts up.

                    comp.Accumulator = comp.MaxAccumulator;
                    RemComp<DamageableComponent>(comp.Owner);
                    comp.State = DragonRiftState.Finished;
                    Dirty(comp);
                }
                else if (comp.State != DragonRiftState.Finished)
                {
                    comp.Accumulator += frameTime;
                }

                comp.SpawnAccumulator += frameTime;

                if (comp.State < DragonRiftState.AlmostFinished && comp.Accumulator > comp.MaxAccumulator / 2f)
                {
                    comp.State = DragonRiftState.AlmostFinished;
                    Dirty(comp);
                    var location = Transform(comp.Owner).LocalPosition;

                    _chat.DispatchGlobalAnnouncement(Loc.GetString("carp-rift-warning", ("location", location)), playSound: false, colorOverride: Color.Red);
                    _audioSystem.PlayGlobal("/Audio/Misc/notice1.ogg", Filter.Broadcast(), true);
                }

                if (comp.SpawnAccumulator > comp.SpawnCooldown)
                {
                    comp.SpawnAccumulator -= comp.SpawnCooldown;
                    var ent = Spawn(comp.SpawnPrototype, Transform(comp.Owner).MapPosition);
                    _npc.SetBlackboard(ent, NPCBlackboard.FollowTarget, new EntityCoordinates(comp.Owner, Vector2.Zero));
                }
            }
        }

        #region Rift

        private void OnRiftExamined(EntityUid uid, DragonRiftComponent component, ExaminedEvent args)
        {
            args.PushMarkup(Loc.GetString("carp-rift-examine", ("percentage", MathF.Round(component.Accumulator / component.MaxAccumulator * 100))));
        }

        private void OnAnchorChange(EntityUid uid, DragonRiftComponent component, ref AnchorStateChangedEvent args)
        {
            if (!args.Anchored && component.State == DragonRiftState.Charging)
            {
                QueueDel(uid);
            }
        }

        private void OnRiftShutdown(EntityUid uid, DragonRiftComponent component, ComponentShutdown args)
        {
            if (TryComp<DragonComponent>(component.Dragon, out var dragon) && !dragon.Weakened)
            {
                foreach (var rift in dragon.Rifts)
                {
                    QueueDel(rift);
                }

                dragon.Rifts.Clear();

                // We can't predict the rift being destroyed anyway so no point adding weakened to shared.
                dragon.WeakenedAccumulator = dragon.WeakenedDuration;
                _movement.RefreshMovementSpeedModifiers(component.Dragon.Value);
                _popupSystem.PopupEntity(Loc.GetString("carp-rift-destroyed"), component.Dragon.Value, component.Dragon.Value);
            }
        }

        private void OnRiftGetState(EntityUid uid, DragonRiftComponent component, ref ComponentGetState args)
        {
            args.State = new DragonRiftComponentState()
            {
                State = component.State
            };
        }

        private void OnDragonMove(EntityUid uid, DragonComponent component, RefreshMovementSpeedModifiersEvent args)
        {
            if (component.Weakened)
            {
                args.ModifySpeed(0.5f, 0.5f);
            }
        }

        private void OnDragonRift(EntityUid uid, DragonComponent component, DragonSpawnRiftActionEvent args)
        {
            if (component.Weakened)
            {
                _popupSystem.PopupEntity(Loc.GetString("carp-rift-weakened"), uid, uid);
                return;
            }

            if (component.Rifts.Count >= RiftsAllowed)
            {
                _popupSystem.PopupEntity(Loc.GetString("carp-rift-max"), uid, uid);
                return;
            }

            if (component.Rifts.Count > 0 && TryComp<DragonRiftComponent>(component.Rifts[^1], out var rift) && rift.State != DragonRiftState.Finished)
            {
                _popupSystem.PopupEntity(Loc.GetString("carp-rift-duplicate"), uid, uid);
                return;
            }

            var xform = Transform(uid);

            // Have to be on a grid fam
            if (!_mapManager.TryGetGrid(xform.GridUid, out var grid))
            {
                _popupSystem.PopupEntity(Loc.GetString("carp-rift-anchor"), uid, uid);
                return;
            }

            foreach (var (_, riftXform) in EntityQuery<DragonRiftComponent, TransformComponent>(true))
            {
                if (riftXform.Coordinates.InRange(EntityManager, xform.Coordinates, RiftRange))
                {
                    _popupSystem.PopupEntity(Loc.GetString("carp-rift-proximity", ("proximity", RiftRange)), uid, uid);
                    return;
                }
            }

            foreach (var tile in grid.GetTilesIntersecting(new Circle(xform.WorldPosition, RiftTileRadius), false))
            {
                if (!tile.IsSpace(_tileDef))
                    continue;

                _popupSystem.PopupEntity(Loc.GetString("carp-rift-space-proximity", ("proximity", RiftTileRadius)), uid, uid);
                return;
            }

            var carpUid = Spawn(component.RiftPrototype, xform.MapPosition);
            component.Rifts.Add(carpUid);
            Comp<DragonRiftComponent>(carpUid).Dragon = uid;
            _audioSystem.PlayPvs("/Audio/Weapons/Guns/Gunshots/rocket_launcher.ogg", carpUid);
        }

        #endregion

        private void OnShutdown(EntityUid uid, DragonComponent component, ComponentShutdown args)
        {
            foreach (var rift in component.Rifts)
            {
                QueueDel(rift);
            }
        }

        private void OnMobStateChanged(EntityUid uid, DragonComponent component, MobStateChangedEvent args)
        {
            //Empties the stomach upon death
            //TODO: Do this when the dragon gets butchered instead
            if (args.NewMobState == MobState.Dead)
            {
                if (component.SoundDeath != null)
                    _audioSystem.PlayPvs(component.SoundDeath, uid, component.SoundDeath.Params);

                component.DragonStomach.EmptyContainer();

                foreach (var rift in component.Rifts)
                {
                    QueueDel(rift);
                }

                component.Rifts.Clear();
            }
        }

        private void Roar(DragonComponent component)
        {
            if (component.SoundRoar != null)
                _audioSystem.Play(component.SoundRoar, Filter.Pvs(component.Owner, 4f, EntityManager), component.Owner, true, component.SoundRoar.Params);
        }

        private void OnStartup(EntityUid uid, DragonComponent component, ComponentStartup args)
        {
            //Dragon doesn't actually chew, since he sends targets right into his stomach.
            //I did it mom, I added ERP content into upstream. Legally!
            component.DragonStomach = _containerSystem.EnsureContainer<Container>(uid, "dragon_stomach");

            if (component.DevourAction != null)
                _actionsSystem.AddAction(uid, component.DevourAction, null);

            if (component.SpawnRiftAction != null)
                _actionsSystem.AddAction(uid, component.SpawnRiftAction, null);

            Roar(component);
        }

        /// <summary>
        /// The devour action
        /// </summary>
        private void OnDevourAction(EntityUid uid, DragonComponent component, DragonDevourActionEvent args)
        {
            if (args.Handled || component.DevourWhitelist?.IsValid(args.Target, EntityManager) != true)
                return;

            args.Handled = true;
            var target = args.Target;

            // Structure and mob devours handled differently.
            if (EntityManager.TryGetComponent(target, out MobStateComponent? targetState))
            {
                switch (targetState.CurrentState)
                {
                    case MobState.Critical:
                    case MobState.Dead:

                        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(uid, component.DevourTime, new DragonDevourDoAfterEvent(), uid, target: target, used: uid)
                        {
                            BreakOnTargetMove = true,
                            BreakOnUserMove = true,
                        });
                        break;
                    default:
                        _popupSystem.PopupEntity(Loc.GetString("devour-action-popup-message-fail-target-alive"), uid, uid);
                        break;
                }

                return;
            }

            _popupSystem.PopupEntity(Loc.GetString("devour-action-popup-message-structure"), uid, uid);

            if (component.SoundStructureDevour != null)
                _audioSystem.PlayPvs(component.SoundStructureDevour, uid, component.SoundStructureDevour.Params);

            _doAfterSystem.TryStartDoAfter(new DoAfterArgs(uid, component.StructureDevourTime, new DragonDevourDoAfterEvent(), uid, target: target, used: uid)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
            });
        }
    }
}
