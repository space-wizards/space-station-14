using Content.Server.Administration.Logs;
using Content.Server.Body.Systems;
using Content.Server.Kitchen.Components;
using Content.Server.Popups;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Kitchen;
using Content.Shared.Kitchen.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using static Content.Shared.Kitchen.Components.KitchenSpikeComponent;

namespace Content.Server.Kitchen.EntitySystems
{
    public sealed class KitchenSpikeSystem : SharedKitchenSpikeSystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
        [Dependency] private readonly IAdminLogManager _logger = default!;
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly TransformSystem _transform = default!;
        [Dependency] private readonly BodySystem _bodySystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly MetaDataSystem _metaData = default!;
        [Dependency] private readonly SharedSuicideSystem _suicide = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<KitchenSpikeComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<KitchenSpikeComponent, InteractHandEvent>(OnInteractHand);
            SubscribeLocalEvent<KitchenSpikeComponent, DragDropTargetEvent>(OnDragDrop);

            //DoAfter
            SubscribeLocalEvent<KitchenSpikeComponent, SpikeDoAfterEvent>(OnDoAfter);

            SubscribeLocalEvent<KitchenSpikeComponent, SuicideByEnvironmentEvent>(OnSuicideByEnvironment);

            SubscribeLocalEvent<ButcherableComponent, CanDropDraggedEvent>(OnButcherableCanDrop);
        }

        private void OnButcherableCanDrop(Entity<ButcherableComponent> entity, ref CanDropDraggedEvent args)
        {
            args.Handled = true;
            args.CanDrop |= entity.Comp.Type != ButcheringType.Knife;
        }

        /// <summary>
        /// TODO: Update this so it actually meatspikes the user instead of applying lethal damage to them.
        /// </summary>
        private void OnSuicideByEnvironment(Entity<KitchenSpikeComponent> entity, ref SuicideByEnvironmentEvent args)
        {
            if (args.Handled)
                return;

            if (!TryComp<DamageableComponent>(args.Victim, out var damageableComponent))
                return;

            _suicide.ApplyLethalDamage((args.Victim, damageableComponent), "Piercing");
            var othersMessage = Loc.GetString("comp-kitchen-spike-suicide-other",
                                                ("victim", Identity.Entity(args.Victim, EntityManager)),
                                                ("this", entity));
            _popupSystem.PopupEntity(othersMessage, args.Victim, Filter.PvsExcept(args.Victim), true);

            var selfMessage = Loc.GetString("comp-kitchen-spike-suicide-self",
                                            ("this", entity));
            _popupSystem.PopupEntity(selfMessage, args.Victim, args.Victim);
            args.Handled = true;
        }

        private void OnDoAfter(Entity<KitchenSpikeComponent> entity, ref SpikeDoAfterEvent args)
        {
            if (args.Args.Target == null)
                return;

            if (TryComp<ButcherableComponent>(args.Args.Target.Value, out var butcherable))
                butcherable.BeingButchered = false;

            if (args.Cancelled)
            {
                entity.Comp.InUse = false;
                return;
            }

            if (args.Handled)
                return;

            if (Spikeable(entity, args.Args.User, args.Args.Target.Value, entity.Comp, butcherable))
                Spike(entity, args.Args.User, args.Args.Target.Value, entity.Comp);

            entity.Comp.InUse = false;
            args.Handled = true;
        }

        private void OnDragDrop(Entity<KitchenSpikeComponent> entity, ref DragDropTargetEvent args)
        {
            if (args.Handled)
                return;

            args.Handled = true;

            if (Spikeable(entity, args.User, args.Dragged, entity.Comp))
                TrySpike(entity, args.User, args.Dragged, entity.Comp);
        }

        private void OnInteractHand(Entity<KitchenSpikeComponent> entity, ref InteractHandEvent args)
        {
            if (args.Handled)
                return;

            if (entity.Comp.PrototypesToSpawn?.Count > 0)
            {
                _popupSystem.PopupEntity(Loc.GetString("comp-kitchen-spike-knife-needed"), entity, args.User);
                args.Handled = true;
            }
        }

        private void OnInteractUsing(Entity<KitchenSpikeComponent> entity, ref InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            if (TryGetPiece(entity, args.User, args.Used))
                args.Handled = true;
        }

        private void Spike(EntityUid uid, EntityUid userUid, EntityUid victimUid,
            KitchenSpikeComponent? component = null, ButcherableComponent? butcherable = null)
        {
            if (!Resolve(uid, ref component) || !Resolve(victimUid, ref butcherable))
                return;

            var logImpact = LogImpact.Medium;
            if (HasComp<HumanoidAppearanceComponent>(victimUid))
                logImpact = LogImpact.Extreme;

            _logger.Add(LogType.Gib, logImpact, $"{ToPrettyString(userUid):user} kitchen spiked {ToPrettyString(victimUid):target}");

            // TODO VERY SUS
            component.PrototypesToSpawn = EntitySpawnCollection.GetSpawns(butcherable.SpawnedEntities, _random);

            // This feels not okay, but entity is getting deleted on "Spike", for now...
            component.MeatSource1p = Loc.GetString("comp-kitchen-spike-remove-meat", ("victim", victimUid));
            component.MeatSource0 = Loc.GetString("comp-kitchen-spike-remove-meat-last", ("victim", victimUid));
            component.Victim = Name(victimUid);

            UpdateAppearance(uid, null, component);

            _popupSystem.PopupEntity(Loc.GetString("comp-kitchen-spike-kill",
                                                    ("user", Identity.Entity(userUid, EntityManager)),
                                                    ("victim", Identity.Entity(victimUid, EntityManager)),
                                                    ("this", uid)),
                                    uid, PopupType.LargeCaution);

            _transform.SetCoordinates(victimUid, Transform(uid).Coordinates);
            // THE WHAT?
            // TODO: Need to be able to leave them on the spike to do DoT, see ss13.
            var gibs = _bodySystem.GibBody(victimUid);
            foreach (var gib in gibs) {
                QueueDel(gib);
            }

            _audio.PlayPvs(component.SpikeSound, uid);
        }

        private bool TryGetPiece(EntityUid uid, EntityUid user, EntityUid used,
            KitchenSpikeComponent? component = null, SharpComponent? sharp = null)
        {
            if (!Resolve(uid, ref component) || component.PrototypesToSpawn == null || component.PrototypesToSpawn.Count == 0)
                return false;

            // Is using knife
            if (!Resolve(used, ref sharp, false) )
            {
                return false;
            }

            var item = _random.PickAndTake(component.PrototypesToSpawn);

            var ent = Spawn(item, Transform(uid).Coordinates);
            _metaData.SetEntityName(ent,
                Loc.GetString("comp-kitchen-spike-meat-name", ("name", Name(ent)), ("victim", component.Victim)));

            if (component.PrototypesToSpawn.Count != 0)
                _popupSystem.PopupEntity(component.MeatSource1p, uid, user, PopupType.MediumCaution);
            else
            {
                UpdateAppearance(uid, null, component);
                _popupSystem.PopupEntity(component.MeatSource0, uid, user, PopupType.MediumCaution);
            }

            return true;
        }

        private void UpdateAppearance(EntityUid uid, AppearanceComponent? appearance = null, KitchenSpikeComponent? component = null)
        {
            if (!Resolve(uid, ref component, ref appearance, false))
                return;

            _appearance.SetData(uid, KitchenSpikeVisuals.Status, component.PrototypesToSpawn?.Count > 0 ? KitchenSpikeStatus.Bloody : KitchenSpikeStatus.Empty, appearance);
        }

        private bool Spikeable(EntityUid uid, EntityUid userUid, EntityUid victimUid,
            KitchenSpikeComponent? component = null, ButcherableComponent? butcherable = null)
        {
            if (!Resolve(uid, ref component))
                return false;

            if (component.PrototypesToSpawn?.Count > 0)
            {
                _popupSystem.PopupEntity(Loc.GetString("comp-kitchen-spike-deny-collect", ("this", uid)), uid, userUid);
                return false;
            }

            if (!Resolve(victimUid, ref butcherable, false))
            {
                _popupSystem.PopupEntity(Loc.GetString("comp-kitchen-spike-deny-butcher", ("victim", Identity.Entity(victimUid, EntityManager)), ("this", uid)), victimUid, userUid);
                return false;
            }

            switch (butcherable.Type)
            {
                case ButcheringType.Spike:
                    return true;
                case ButcheringType.Knife:
                    _popupSystem.PopupEntity(Loc.GetString("comp-kitchen-spike-deny-butcher-knife", ("victim", Identity.Entity(victimUid, EntityManager)), ("this", uid)), victimUid, userUid);
                    return false;
                default:
                    _popupSystem.PopupEntity(Loc.GetString("comp-kitchen-spike-deny-butcher", ("victim", Identity.Entity(victimUid, EntityManager)), ("this", uid)), victimUid, userUid);
                    return false;
            }
        }

        public bool TrySpike(EntityUid uid, EntityUid userUid, EntityUid victimUid, KitchenSpikeComponent? component = null,
            ButcherableComponent? butcherable = null, MobStateComponent? mobState = null)
        {
            if (!Resolve(uid, ref component) || component.InUse ||
                !Resolve(victimUid, ref butcherable) || butcherable.BeingButchered)
                return false;

            // THE WHAT? (again)
            // Prevent dead from being spiked TODO: Maybe remove when rounds can be played and DOT is implemented
            if (Resolve(victimUid, ref mobState, false) &&
                _mobStateSystem.IsAlive(victimUid, mobState))
            {
                _popupSystem.PopupEntity(Loc.GetString("comp-kitchen-spike-deny-not-dead", ("victim", Identity.Entity(victimUid, EntityManager))),
                    victimUid, userUid);
                return true;
            }

            if (userUid != victimUid)
            {
                _popupSystem.PopupEntity(Loc.GetString("comp-kitchen-spike-begin-hook-victim", ("user", Identity.Entity(userUid, EntityManager)), ("this", uid)), victimUid, victimUid, PopupType.LargeCaution);
            }
            // TODO: make it work when SuicideEvent is implemented
            // else
            //    _popupSystem.PopupEntity(Loc.GetString("comp-kitchen-spike-begin-hook-self", ("this", uid)), victimUid, Filter.Pvs(uid)); // This is actually unreachable and should be in SuicideEvent

            butcherable.BeingButchered = true;
            component.InUse = true;

            var doAfterArgs = new DoAfterArgs(EntityManager, userUid, component.SpikeDelay + butcherable.ButcherDelay, new SpikeDoAfterEvent(), uid, target: victimUid, used: uid)
            {
                BreakOnDamage = true,
                BreakOnMove = true,
                NeedHand = true,
                BreakOnDropItem = false,
            };

            _doAfter.TryStartDoAfter(doAfterArgs);

            return true;
        }
    }
}
