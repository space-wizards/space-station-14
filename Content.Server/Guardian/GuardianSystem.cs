using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Audio;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.MobState.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Guardian
{
    public sealed class GuardianSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly DamageableSystem _damageSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<GuardianCreatorComponent, UseInHandEvent>(OnCreatorUse);
            SubscribeLocalEvent<GuardianCreatorComponent, ExaminedEvent>(OnCreatorExamine);

            SubscribeLocalEvent<GuardianComponent, MoveEvent>(OnGuardianMove);
            SubscribeLocalEvent<GuardianComponent, DamageChangedEvent>(OnGuardianDamaged);

            SubscribeLocalEvent<GuardianHostComponent, ComponentInit>(OnHostInit);
            SubscribeLocalEvent<GuardianHostComponent, MoveEvent>(OnHostMove);
            SubscribeLocalEvent<GuardianHostComponent, MobStateChangedEvent>(OnHostStateChange);
            SubscribeLocalEvent<GuardianHostComponent, ComponentShutdown>(OnHostShutdown);
        }

        private void OnHostShutdown(EntityUid uid, GuardianHostComponent component, ComponentShutdown args)
        {
            if (!component.HostedGuardian.IsValid()) return;
            EntityManager.QueueDeleteEntity(component.HostedGuardian);
        }

        private void OnHostInit(EntityUid uid, GuardianHostComponent component, ComponentInit args)
        {
            component.GuardianContainer = uid.EnsureContainer<ContainerSlot>("GuardianContainer");
        }

        /// <summary>
        /// Adds the guardian host component to the user and spawns the guardian inside said component
        /// </summary>
        private void OnCreatorUse(EntityUid uid, GuardianCreatorComponent component, UseInHandEvent args)
        {
            if (component.Used)
            {
                _popupSystem.PopupEntity(Loc.GetString("guardian-activator-empty-invalid-creation"), args.User, Filter.Entities(args.User));
                return;
            }

            // If user is already a host don't duplicate.
            if (EntityManager.HasComponent<GuardianHostComponent>(args.User))
            {
                _popupSystem.PopupEntity(Loc.GetString("guardian-already-present-invalid-creation"), args.User, Filter.Entities(args.User));
                return;
            }

            // Can't work without actions
            if (!EntityManager.TryGetComponent(args.User, out SharedActionsComponent? actions))
            {
                _popupSystem.PopupEntity(Loc.GetString("guardian-no-actions-invalid-creation"), args.User, Filter.Entities(args.User));
                return;
            }

            var hostXform = EntityManager.GetComponent<TransformComponent>(uid);

            var host = EntityManager.EnsureComponent<GuardianHostComponent>(args.User);
            // Use map position so it's not inadvertantly parented to the host + if it's in a container it spawns outside I guess.
            var guardian = EntityManager.SpawnEntity(component.GuardianProto, hostXform.MapPosition);

            host.GuardianContainer.Insert(guardian);
            host.HostedGuardian = guardian;

            if (EntityManager.TryGetComponent(guardian, out GuardianComponent? guardianComponent))
            {
                guardianComponent.Host = args.User;

                // Grant the user the recall action and notify them
                actions.Grant(ActionType.ManifestGuardian);
                SoundSystem.Play(Filter.Entities(uid), "/Audio/Effects/guardian_inject.ogg", uid);

                _popupSystem.PopupEntity(Loc.GetString("guardian-created"), args.User, Filter.Entities(args.User));
                // Exhaust the activator
                component.Used = true;
            }
            else
            {
                Logger.ErrorS("guardian", $"Tried to spawn a guardian that doesn't have {nameof(GuardianComponent)}");
                EntityManager.QueueDeleteEntity(guardian);
            }
        }


        /// <summary>
        /// Triggers when the host receives damage which puts the host in either critical or killed state
        /// </summary>
        private void OnHostStateChange(EntityUid uid, GuardianHostComponent component, MobStateChangedEvent args)
        {
            if (!component.HostedGuardian.IsValid()) return;

            if (args.State.IsCritical())
            {
                _popupSystem.PopupEntity(Loc.GetString("guardian-host-critical-warn"), component.HostedGuardian, Filter.Entities(component.HostedGuardian));
                SoundSystem.Play(Filter.Entities(component.HostedGuardian), "/Audio/Effects/guardian_warn.ogg", component.HostedGuardian);
            }
            else if (args.State.IsDead())
            {
                SoundSystem.Play(Filter.Pvs(component.HostedGuardian), "/Audio/Voice/Human/malescream_guardian.ogg", uid, AudioHelpers.WithVariation(0.20f));
                EntityManager.RemoveComponent<GuardianHostComponent>(uid);
            }
        }

        /// <summary>
        /// Handles guardian receiving damage and splitting it with the host according to his defence percent
        /// </summary>
        private void OnGuardianDamaged(EntityUid uid, GuardianComponent component, DamageChangedEvent args)
        {
            if (args.DamageDelta == null ||
                !EntityManager.HasComponent<DamageableComponent>(uid) ||
                !EntityManager.TryGetComponent<DamageableComponent>(component.Host, out var hostDamage)) return;

            _damageSystem.SetDamage(hostDamage, (hostDamage.Damage + args.DamageDelta * component.DamageShare));
            _popupSystem.PopupEntity(Loc.GetString("guardian-entity-taking-damage"), component.Host, Filter.Entities(component.Host));

        }

        /// <summary>
        /// Triggers while trying to examine an activator to see if it's used
        /// </summary>
        private void OnCreatorExamine(EntityUid uid, GuardianCreatorComponent component, ExaminedEvent args)
        {
           if (component.Used)
           {
               args.PushMarkup(Loc.GetString("guardian-activator-empty-invalid-creation"));
           }
        }

        /// <summary>
        /// Called every time the host moves, to make sure the distance between the host and the guardian isn't too far
        /// </summary>
        private void OnHostMove(EntityUid uid, GuardianHostComponent component, ref MoveEvent args)
        {
            if (!EntityManager.TryGetComponent(component.HostedGuardian, out GuardianComponent? guardianComponent) ||
                !guardianComponent.GuardianLoose) return;

            CheckGuardianMove(uid, component.HostedGuardian, component);
        }

        /// <summary>
        /// Called every time the guardian moves: makes sure it's not out of it's allowed distance
        /// </summary>
        private void OnGuardianMove(EntityUid uid, GuardianComponent component, ref MoveEvent args)
        {
            if (!component.GuardianLoose) return;

            CheckGuardianMove(component.Host, uid, guardianComponent: component);
        }

        /// <summary>
        /// Retract the guardian if either the host or the guardian move away from each other.
        /// </summary>
        private void CheckGuardianMove(
            EntityUid hostUid,
            EntityUid guardianUid,
            GuardianHostComponent? hostComponent = null,
            GuardianComponent? guardianComponent = null,
            TransformComponent? hostXform = null,
            TransformComponent? guardianXform = null)
        {
            if (!Resolve(hostUid, ref hostComponent) ||
                !Resolve(guardianUid, ref guardianComponent) ||
                !Resolve(hostUid, ref hostXform) ||
                !Resolve(guardianUid, ref guardianXform))
            {
                return;
            }

            if (!guardianComponent.GuardianLoose) return;

            if (!guardianXform.Coordinates.InRange(EntityManager, hostXform.Coordinates, guardianComponent.DistanceAllowed))
            {
                RetractGuardian(hostComponent, guardianComponent);
            }
        }

        private void ReleaseGuardian(GuardianHostComponent hostComponent, GuardianComponent guardianComponent)
        {
            if (guardianComponent.GuardianLoose)
            {
                DebugTools.Assert(!hostComponent.GuardianContainer.Contains(guardianComponent.Owner));
                return;
            }

            DebugTools.Assert(hostComponent.GuardianContainer.Contains(guardianComponent.Owner));
            hostComponent.GuardianContainer.Remove(guardianComponent.Owner);
            DebugTools.Assert(!hostComponent.GuardianContainer.Contains(guardianComponent.Owner));

            guardianComponent.GuardianLoose = true;
        }

        private void RetractGuardian(GuardianHostComponent hostComponent, GuardianComponent guardianComponent)
        {
            if (!guardianComponent.GuardianLoose)
            {
                DebugTools.Assert(hostComponent.GuardianContainer.Contains(guardianComponent.Owner));
                return;
            }

            hostComponent.GuardianContainer.Insert(guardianComponent.Owner);
            DebugTools.Assert(hostComponent.GuardianContainer.Contains(guardianComponent.Owner));
            _popupSystem.PopupEntity(Loc.GetString("guardian-entity-recall"), hostComponent.Owner, Filter.Pvs(hostComponent.Owner));
            guardianComponent.GuardianLoose = false;
        }
    }
}
