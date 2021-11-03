using Content.Server.Actions.Actions;
using Content.Server.Lathe.Components;
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.MobState;
using Content.Shared.MobState.Components;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using System;

namespace Content.Server.Guardian
{
    public class GuardianSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<GuardianCreatorComponent, UseInHandEvent>(OnGuardianCreated);
            SubscribeLocalEvent<GuardianComponent, MoveEvent>(OnGuardianMove);
            SubscribeLocalEvent<GuardianHostComponent, MoveEvent>(OnGuardianHostMove);
            SubscribeLocalEvent<GuardianComponent, DamageChangedEvent>(OnGuardianDamaged);
            SubscribeLocalEvent<GuardianHostComponent, MobStateChangedMessage>(OnHostDeath);
        }

        /// <summary>
        /// Triggers upon mobstate, to detect disintigration command upon host's death
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="component"></param>
        /// <param name="args"></param>
        private void OnHostDeath(EntityUid uid, GuardianHostComponent component, MobStateChangedMessage args)
        {
            //We only care if the host is dead, not in crit or incapacitated, as he still can support a holopara
            //Dragging him away
            if (component.Owner.GetComponent<MobStateComponent>().IsDead())
            {
                //Delete both entities to prevent revival, guardian first to avoid errors
                //TODO: add disintigration. Proper. Current method leaves no items behind.
                EntityManager.QueueDeleteEntity(component._hostedguardian);
                EntityManager.QueueDeleteEntity(uid);
            }
        }

        /// <summary>
        /// Triggers upon guardian taking damage, reflecting it to the host
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="component"></param>
        /// <param name="args"></param>
        private void OnGuardianDamaged(EntityUid uid, GuardianComponent component, DamageChangedEvent args)
        {
            var guardiandamage = EntityManager.GetEntity(uid).EnsureComponent<DamageableComponent>();
            var hostdamage = EntityManager.GetEntity(component.Host).EnsureComponent<DamageableComponent>();
            if (args.DamageDelta != null)
            {
                EntitySystem.Get<DamageableSystem>().SetDamage(hostdamage, (hostdamage.Damage+args.DamageDelta*component.DamagePercent));
                hostdamage.Owner.PopupMessage(Loc.GetString("guardian-entity-taking-damage"));
            }
                  
        }



        /// <summary>
        /// Called every time the host moves, to make sure the distance between the host and the guardian isn't too far
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="component"></param>
        /// <param name="args"></param>
        private void OnGuardianHostMove(EntityUid uid, GuardianHostComponent component, ref MoveEvent args)
        {
            var guardcomp = EntityManager.GetEntity(component._hostedguardian).GetComponent<GuardianComponent>();
            if (guardcomp.Guardianloose == true)
            {
                var host = EntityManager.GetEntity(uid);
                var guardian = EntityManager.GetEntity(guardcomp.Guardian);
                //Compares the distance to allowed distance, otherwise forces a recall action from the host
                if (!guardian.Transform.Coordinates.InRange(EntityManager, host.Transform.Coordinates, guardcomp.DistanceAllowed))
                {
                    OnGuardianManifestAction(guardian.Uid, uid);
                }
            }
        }

        /// <summary>
        /// Called every time the guardian moves: makes sure it's not out of it's allowed distance
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="component"></param>
        /// <param name="args"></param>
        private void OnGuardianMove(EntityUid uid, GuardianComponent component, ref MoveEvent args)
        {
            if (component.Guardianloose == true)
            {
                var guardian = EntityManager.GetEntity(uid);
                var host = EntityManager.GetEntity(component.Host);
                //Compares the distance to allowed distance, otherwise forces a recall action from the host
                if (!host.Transform.Coordinates.InRange(EntityManager, guardian.Transform.Coordinates, component.DistanceAllowed))
                {
                    OnGuardianManifestAction(uid, host.Uid);
                }
            }
        }

        /// <summary>
        /// Adds the guardian host component to the user and spawns the guardian inside said component
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="component"></param>
        /// <param name="args"></param>
        private void OnGuardianCreated(EntityUid uid, GuardianCreatorComponent component, UseInHandEvent args)
        {
            //Only works if the guardian isn't already present. NOTE: this is up to preference, we can probably later allow
            //a person to host multiple guardians, however the idea is that in a normal setting you won't get the opportunity to
            if (!args.User.TryGetComponent<GuardianHostComponent>(out var guardianComponent))
            {
                if (args.User.TryGetComponent<SharedActionsComponent>(out SharedActionsComponent? action))
                {
                    var hostcomp = args.User.AddComponent<GuardianHostComponent>();
                    var guardian = EntityManager.SpawnEntity(component.GuardianType, hostcomp.Owner.Transform.Coordinates);
                    hostcomp.GuardianContainer.Insert(guardian);
                    hostcomp._hostedguardian = guardian.Uid;
                    //Ensures the guardian component on the supposed guardian entity and fills out it's synced parametres
                    var guardiancomp = guardian.EnsureComponent<GuardianComponent>();
                    guardiancomp.Guardian = guardian.Uid;
                    guardiancomp.Host = args.User.Uid;
                    //Grant the user the recall action and notify them
                    action.Grant(ActionType.ManifestGuardian);
                    args.User.PopupMessage(Loc.GetString("guardian-created"));     
                }
                else
                {
                    args.User.PopupMessage(Loc.GetString("guardian-no-actions-invalid-creation"));
                }
                }
                else
                {
                    args.User.PopupMessage(Loc.GetString("guardian-already-present-invalid-creation"));
                }
            }

        public void OnGuardianManifestAction(EntityUid guardian, EntityUid host)
        {
            var guardianloose = EntityManager.GetEntity(guardian).GetComponent<GuardianComponent>().Guardianloose;
            //the loose parameter toggling is inside the shared component
            if (guardianloose == false)
            {
                //Ejects the guardian if it isn't inside
                EntityManager.GetEntity(host).GetComponent<GuardianHostComponent>().EjectBody();
            }
            else if (guardianloose == true)
            {
                //Recalls guardian if it's outside
                //Message first otherwise it's a dead giveaway
                EntityManager.GetEntity(guardian).PopupMessageEveryone(Loc.GetString("guardian-entity-recall"));
                EntityManager.GetEntity(host).GetComponent<GuardianHostComponent>().GuardianContainer.Insert(EntityManager.GetEntity(guardian));         
            }
            //Update the guardian loose value
            EntityManager.GetEntity(guardian).GetComponent<GuardianComponent>().Guardianloose = !guardianloose;
        }
    }
}
