using Content.Server.Administration.Logs;
using Content.Server.DoAfter;
using Content.Server.Kitchen.Components;
using Content.Server.MobState;
using Content.Server.Nutrition.Components;
using Content.Server.Popups;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DragDrop;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.MobState.Components;
using Content.Shared.Nutrition.Components;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Content.Shared.Storage;
using Robust.Shared.Random;
using static Content.Shared.Kitchen.Components.SharedKitchenSpikeComponent;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;

namespace Content.Server.Kitchen.EntitySystems
{
    internal sealed class KitchenSpikeSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly DoAfterSystem _doAfter = default!;
        [Dependency] private readonly IAdminLogManager _logger = default!;
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<KitchenSpikeComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<KitchenSpikeComponent, InteractHandEvent>(OnInteractHand);
            SubscribeLocalEvent<KitchenSpikeComponent, DragDropEvent>(OnDragDrop);

            //DoAfter
            SubscribeLocalEvent<KitchenSpikeComponent, SpikingFinishedEvent>(OnSpikingFinished);
            SubscribeLocalEvent<KitchenSpikeComponent, SpikingFailEvent>(OnSpikingFail);

            SubscribeLocalEvent<KitchenSpikeComponent, SuicideEvent>(OnSuicide);
        }

        private void OnSuicide(EntityUid uid, KitchenSpikeComponent component, SuicideEvent args)
        {
            if (args.Handled) return;
            args.SetHandled(SuicideKind.Piercing);
            var victim = args.Victim;
            var othersMessage = Loc.GetString("comp-kitchen-spike-suicide-other", ("victim", victim));
            victim.PopupMessageOtherClients(othersMessage);

            var selfMessage = Loc.GetString("comp-kitchen-spike-suicide-self");
            victim.PopupMessage(selfMessage);
        }

        private void OnSpikingFail(EntityUid uid, KitchenSpikeComponent component, SpikingFailEvent args)
        {
            component.InUse = false;

            if (EntityManager.TryGetComponent<SharedButcherableComponent>(args.VictimUid, out var butcherable))
                butcherable.BeingButchered = false;
        }

        private void OnSpikingFinished(EntityUid uid, KitchenSpikeComponent component, SpikingFinishedEvent args)
        {
            component.InUse = false;

            if (EntityManager.TryGetComponent<SharedButcherableComponent>(args.VictimUid, out var butcherable))
                butcherable.BeingButchered = false;

            if (Spikeable(uid, args.UserUid, args.VictimUid, component, butcherable))
            {
                Spike(uid, args.UserUid, args.VictimUid, component);
            }
        }

        private void OnDragDrop(EntityUid uid, KitchenSpikeComponent component, DragDropEvent args)
        {
            if(args.Handled)
                return;

            args.Handled = true;

            if (Spikeable(uid, args.User, args.Dragged, component))
                TrySpike(uid, args.User, args.Dragged, component);

        }
        private void OnInteractHand(EntityUid uid, KitchenSpikeComponent component, InteractHandEvent args)
        {
            if (args.Handled)
                return;

            if (component.PrototypesToSpawn?.Count > 0) {
                _popupSystem.PopupEntity(Loc.GetString("comp-kitchen-spike-knife-needed"), uid, args.User);
                args.Handled = true;
            }
        }

        private void OnInteractUsing(EntityUid uid, KitchenSpikeComponent component, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            if (TryGetPiece(uid, args.User, args.Used))
                args.Handled = true;
        }

        private void Spike(EntityUid uid, EntityUid userUid, EntityUid victimUid,
            KitchenSpikeComponent? component = null, SharedButcherableComponent? butcherable = null)
        {
            if (!Resolve(uid, ref component) || !Resolve(victimUid, ref butcherable))
                return;

            _logger.Add(LogType.Gib, LogImpact.Extreme, $"{ToPrettyString(userUid):user} kitchen spiked {ToPrettyString(victimUid):target}");

            // TODO VERY SUS
            component.PrototypesToSpawn = EntitySpawnCollection.GetSpawns(butcherable.SpawnedEntities, _random);

            // This feels not okay, but entity is getting deleted on "Spike", for now...
            component.MeatSource1p = Loc.GetString("comp-kitchen-spike-remove-meat", ("victim", victimUid));
            component.MeatSource0 = Loc.GetString("comp-kitchen-spike-remove-meat-last", ("victim", victimUid));
            component.Victim = Name(victimUid);

            UpdateAppearance(uid, null, component);

            _popupSystem.PopupEntity(Loc.GetString("comp-kitchen-spike-kill", ("user", Identity.Entity(userUid, EntityManager)), ("victim", victimUid)), uid, PopupType.LargeCaution);

            // THE WHAT?
            // TODO: Need to be able to leave them on the spike to do DoT, see ss13.
            EntityManager.QueueDeleteEntity(victimUid);

            SoundSystem.Play(component.SpikeSound.GetSound(), Filter.Pvs(uid), uid);
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
            MetaData(ent).EntityName =
                Loc.GetString("comp-kitchen-spike-meat-name", ("name", Name(ent)), ("victim", component.Victim));

            if (component.PrototypesToSpawn.Count != 0)
            {
                _popupSystem.PopupEntity(component.MeatSource1p, uid, user, PopupType.MediumCaution);
            }
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

            appearance.SetData(KitchenSpikeVisuals.Status, (component.PrototypesToSpawn?.Count > 0) ? KitchenSpikeStatus.Bloody : KitchenSpikeStatus.Empty);
        }

        private bool Spikeable(EntityUid uid, EntityUid userUid, EntityUid victimUid,
            KitchenSpikeComponent? component = null, SharedButcherableComponent? butcherable = null)
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
            SharedButcherableComponent? butcherable = null, MobStateComponent? mobState = null)
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

            var doAfterArgs = new DoAfterEventArgs(userUid, component.SpikeDelay + butcherable.ButcherDelay, default, uid)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnDamage = true,
                BreakOnStun = true,
                NeedHand = true,
                TargetFinishedEvent = new SpikingFinishedEvent(userUid, victimUid),
                TargetCancelledEvent = new SpikingFailEvent(victimUid)
            };

            _doAfter.DoAfter(doAfterArgs);

            return true;
        }

        private sealed class SpikingFinishedEvent : EntityEventArgs
        {
            public EntityUid VictimUid;
            public EntityUid UserUid;

            public SpikingFinishedEvent(EntityUid userUid, EntityUid victimUid)
            {
                UserUid = userUid;
                VictimUid = victimUid;
            }
        }

        private sealed class SpikingFailEvent : EntityEventArgs
        {
            public EntityUid VictimUid;

            public SpikingFailEvent(EntityUid victimUid)
            {
                VictimUid = victimUid;
            }
        }
    }
}
