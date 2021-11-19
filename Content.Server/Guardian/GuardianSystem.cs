using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Audio;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.MobState.EntitySystems;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;

namespace Content.Server.Guardian
{
    public class GuardianSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<GuardianCreatorComponent, UseInHandEvent>(OnGuardianCreated);
            SubscribeLocalEvent<GuardianCreatorComponent, ExaminedEvent>(OnActivatorExamined);
            SubscribeLocalEvent<GuardianComponent, MoveEvent>(OnGuardianMove);
            SubscribeLocalEvent<GuardianHostComponent, MoveEvent>(OnGuardianHostMove);
            SubscribeLocalEvent<GuardianComponent, DamageChangedEvent>(OnGuardianDamaged);
            SubscribeLocalEvent<GuardianHostComponent, MobStateChangedEvent>(OnHostStateChange);
        }

        /// <summary>
        /// Triggers when the host recives damage which puts the host in either critical or killed state
        /// </summary>
        private void OnHostStateChange(EntityUid uid, GuardianHostComponent component, MobStateChangedEvent args)
        {
            if (EntityManager.GetEntity(component.Hostedguardian) != null)
            {
                if (args.State.IsCritical())
                {
                    var guard = EntityManager.GetEntity(component.Hostedguardian);
                    _popupSystem.PopupCoordinates(Loc.GetString("guardian-host-critical-warn"), guard.Transform.Coordinates, Filter.Entities(guard.Uid));
                    SoundSystem.Play(Filter.Local(), "/Audio/Effects/guardian_warn.ogg", uid);
                }
                else if (args.State.IsDead())
                {
                    var guard = EntityManager.GetEntity(component.Hostedguardian);
                    SoundSystem.Play(Filter.Pvs(guard), "/Audio/Voice/Human/malescream_guardian.ogg", uid, AudioHelpers.WithVariation(0.20f));
                    EntityManager.QueueDeleteEntity(guard);
                    EntityManager.QueueDeleteEntity(uid);
                }
            }
        }

        private void OnGuardianDamaged(EntityUid uid, GuardianComponent component, DamageChangedEvent args)
        {
            if (EntityManager.GetEntity(uid).TryGetComponent<DamageableComponent>(out DamageableComponent? guardiandamage))
            {
                EntityManager.GetEntity(component.Host).TryGetComponent<DamageableComponent>(out DamageableComponent? hostdamage);
                if (hostdamage != null)
                {
                    if (args.DamageDelta != null)
                    {
                        EntitySystem.Get<DamageableSystem>().SetDamage(hostdamage, (hostdamage.Damage + args.DamageDelta * component.DamagePercent));
                        _popupSystem.PopupCoordinates(Loc.GetString("guardian-entity-taking-damage"), hostdamage.Owner.Transform.Coordinates, Filter.Entities(hostdamage.OwnerUid));
                    }
                }
            }
        }

        /// <summary>
        /// Triggers while trying to examine an activator to see if it's used
        /// </summary>
        private void OnActivatorExamined(EntityUid uid, GuardianCreatorComponent component, ExaminedEvent args)
        {
           if (component.Used == true)
           {
               string usedstring = Loc.GetString("guardian-activator-empty-invalid-creaton");
               args.Message.AddMarkup("\n" + $"[color=#ba1919]{usedstring}[/color]");
           }
        }

        /// <summary>
        /// Adds the guardian host component to the user and spawns the guardian inside said component
        /// </summary>
        private void OnGuardianCreated(EntityUid uid, GuardianCreatorComponent component, UseInHandEvent args)
        {
            //Only works if the guardian isn't already present. NOTE: this is up to preference, we can probably later allow
            //a person to host multiple guardians, however the idea is that in a normal setting you won't get the opportunity to
            if (!args.User.TryGetComponent<GuardianHostComponent>(out var guardianComponent))
            {
                if (args.User.TryGetComponent<SharedActionsComponent>(out SharedActionsComponent? action))
                {
                    if (component.Used == false)
                    {
                        var hostcomp = args.User.AddComponent<GuardianHostComponent>();
                        var guardian = EntityManager.SpawnEntity(component.GuardianType, hostcomp.Owner.Transform.Coordinates);
                        hostcomp.GuardianContainer.Insert(guardian);
                        hostcomp.Hostedguardian = guardian.Uid;
                        //Takes the guardian component on the supposed guardian entity and fills out it's synced parametres
                        if (guardian.TryGetComponent<GuardianComponent>(out GuardianComponent? guardiancomp))
                        {
                            guardiancomp.Host = args.User.Uid;
                            //Grant the user the recall action and notify them
                            action.Grant(ActionType.ManifestGuardian);
                            SoundSystem.Play(Filter.Entities(uid), "/Audio/Effects/guardian_inject.ogg", uid);
                            _popupSystem.PopupCoordinates(Loc.GetString("guardian-created"), hostcomp.Owner.Transform.Coordinates, Filter.Entities(args.User.Uid));
                            //Exhaust the activator
                            component.Used = true;
                        }                   
                    }
                    else
                    {
                        _popupSystem.PopupCoordinates(Loc.GetString("guardian-activator-empty-invalid-creaton"), args.User.Transform.Coordinates, Filter.Entities(args.User.Uid));
                    }
                }
                else
                {
                    _popupSystem.PopupCoordinates(Loc.GetString("guardian-no-actions-invalid-creation"), args.User.Transform.Coordinates, Filter.Entities(args.User.Uid));
                }
            }
            else
            {
                _popupSystem.PopupCoordinates(Loc.GetString("guardian-already-present-invalid-creation"), args.User.Transform.Coordinates, Filter.Entities(args.User.Uid));
            }
        }

        /// <summary>
        /// Called every time the host moves, to make sure the distance between the host and the guardian isn't too far
        /// </summary>
        private void OnGuardianHostMove(EntityUid uid, GuardianHostComponent component, ref MoveEvent args)
        {
            if (EntityManager.GetEntity(component.Hostedguardian).TryGetComponent<GuardianComponent>(out GuardianComponent? guardcomp))
            {
                if (guardcomp.guardianLoose == true)
                {
                    //Compares the distance to allowed distance, otherwise forces a recall action from the host
                    if (!guardcomp.Owner.Transform.Coordinates.InRange(EntityManager, component.Owner.Transform.Coordinates, guardcomp.DistanceAllowed))
                    {
                        OnGuardianManifestAction(guardcomp.OwnerUid, uid);
                    }
                }
            }
        }

        /// <summary>
        /// Called every time the guardian moves: makes sure it's not out of it's allowed distance
        /// </summary>
        private void OnGuardianMove(EntityUid uid, GuardianComponent component, ref MoveEvent args)
        {
            if (component.guardianLoose == true)
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

        public void OnGuardianManifestAction(EntityUid guardian, EntityUid host)
        {
            var guardianloose = EntityManager.GetEntity(guardian).GetComponent<GuardianComponent>().guardianLoose;
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
                _popupSystem.PopupCoordinates(Loc.GetString("guardian-entity-recall"), EntityManager.GetEntity(guardian).Transform.Coordinates, Filter.Pvs(EntityManager.GetEntity(guardian)));
                EntityManager.GetEntity(host).GetComponent<GuardianHostComponent>().GuardianContainer.Insert(EntityManager.GetEntity(guardian));
            }
            //Update the guardian loose value
            EntityManager.GetEntity(guardian).GetComponent<GuardianComponent>().guardianLoose = !guardianloose;
        }
    }
}
