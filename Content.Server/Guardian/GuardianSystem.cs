using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.Audio;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.MobState;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Guardian
{
    /// <summary>
    /// A guardian has a host it's attached to that it fights for. A fighting spirit.
    /// </summary>
    public sealed class GuardianSystem : EntitySystem
    {
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly DamageableSystem _damageSystem = default!;
        [Dependency] private readonly SharedActionsSystem _actionSystem = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<GuardianCreatorComponent, UseInHandEvent>(OnCreatorUse);
            SubscribeLocalEvent<GuardianCreatorComponent, AfterInteractEvent>(OnCreatorInteract);
            SubscribeLocalEvent<GuardianCreatorComponent, ExaminedEvent>(OnCreatorExamine);
            SubscribeLocalEvent<GuardianCreatorInjectedEvent>(OnCreatorInject);
            SubscribeLocalEvent<GuardianCreatorInjectCancelledEvent>(OnCreatorCancelled);

            SubscribeLocalEvent<GuardianComponent, MoveEvent>(OnGuardianMove);
            SubscribeLocalEvent<GuardianComponent, DamageChangedEvent>(OnGuardianDamaged);
            SubscribeLocalEvent<GuardianComponent, PlayerAttachedEvent>(OnGuardianPlayer);
            SubscribeLocalEvent<GuardianComponent, PlayerDetachedEvent>(OnGuardianUnplayer);

            SubscribeLocalEvent<GuardianHostComponent, ComponentInit>(OnHostInit);
            SubscribeLocalEvent<GuardianHostComponent, MoveEvent>(OnHostMove);
            SubscribeLocalEvent<GuardianHostComponent, MobStateChangedEvent>(OnHostStateChange);
            SubscribeLocalEvent<GuardianHostComponent, ComponentShutdown>(OnHostShutdown);

            SubscribeLocalEvent<GuardianHostComponent, GuardianToggleActionEvent>(OnPerformAction);

            SubscribeLocalEvent<GuardianComponent, AttackAttemptEvent>(OnGuardianAttackAttempt);
        }

        private void OnPerformAction(EntityUid uid, GuardianHostComponent component, GuardianToggleActionEvent args)
        {
            if (args.Handled)
                return;

            if (component.HostedGuardian != null)
                ToggleGuardian(component);

            args.Handled = true;
        }

        private void OnGuardianUnplayer(EntityUid uid, GuardianComponent component, PlayerDetachedEvent args)
        {
            var host = component.Host;

            if (!TryComp<GuardianHostComponent>(host, out var hostComponent)) return;

            if (LifeStage(host) >= EntityLifeStage.MapInitialized)
                return;

            RetractGuardian(hostComponent, component);
        }

        private void OnGuardianPlayer(EntityUid uid, GuardianComponent component, PlayerAttachedEvent args)
        {
            var host = component.Host;

            if (!HasComp<GuardianHostComponent>(host)) return;

            _popupSystem.PopupEntity(Loc.GetString("guardian-available"), host, host);
        }

        private void OnHostInit(EntityUid uid, GuardianHostComponent component, ComponentInit args)
        {
            component.GuardianContainer = uid.EnsureContainer<ContainerSlot>("GuardianContainer");
            _actionSystem.AddAction(uid, component.Action, null);
        }

        private void OnHostShutdown(EntityUid uid, GuardianHostComponent component, ComponentShutdown args)
        {
            if (component.HostedGuardian == null) return;
            EntityManager.QueueDeleteEntity(component.HostedGuardian.Value);
            _actionSystem.RemoveAction(uid, component.Action);
        }

        private void OnGuardianAttackAttempt(EntityUid uid, GuardianComponent component, AttackAttemptEvent args)
        {
            if (args.Cancelled || args.Target != component.Host)
                return;

            // why is this server side code? This should be in shared
            _popupSystem.PopupCursor(Loc.GetString("guardian-attack-host"), uid, PopupType.LargeCaution);
            args.Cancel();
        }

        public void ToggleGuardian(GuardianHostComponent hostComponent)
        {
            if (hostComponent.HostedGuardian == null ||
                !TryComp(hostComponent.HostedGuardian, out GuardianComponent? guardianComponent)) return;

            if (guardianComponent.GuardianLoose)
            {
                RetractGuardian(hostComponent, guardianComponent);
            }
            else
            {
                ReleaseGuardian(hostComponent, guardianComponent);
            }
        }

        /// <summary>
        /// Adds the guardian host component to the user and spawns the guardian inside said component
        /// </summary>
        private void OnCreatorUse(EntityUid uid, GuardianCreatorComponent component, UseInHandEvent args)
        {
            if (args.Handled) return;
            args.Handled = true;
            UseCreator(args.User, args.User, component);
        }

        private void OnCreatorInteract(EntityUid uid, GuardianCreatorComponent component, AfterInteractEvent args)
        {
            if (args.Handled || args.Target == null || !args.CanReach) return;
            args.Handled = true;
            UseCreator(args.User, args.Target.Value, component);
        }

        private void OnCreatorCancelled(GuardianCreatorInjectCancelledEvent ev)
        {
            ev.Component.Injecting = false;
        }

        private void UseCreator(EntityUid user, EntityUid target, GuardianCreatorComponent component)
        {
            if (component.Used)
            {
                _popupSystem.PopupEntity(Loc.GetString("guardian-activator-empty-invalid-creation"), user, user);
                return;
            }

            // Can only inject things with the component...
            if (!HasComp<CanHostGuardianComponent>(target))
            {
                _popupSystem.PopupEntity(Loc.GetString("guardian-activator-invalid-target"), user, user);
                return;
            }

            // If user is already a host don't duplicate.
            if (HasComp<GuardianHostComponent>(target))
            {
                _popupSystem.PopupEntity(Loc.GetString("guardian-already-present-invalid-creation"), user, user);
                return;
            }

            if (component.Injecting) return;

            component.Injecting = true;

            _doAfterSystem.DoAfter(new DoAfterEventArgs(user, component.InjectionDelay, target: target)
            {
                BroadcastFinishedEvent = new GuardianCreatorInjectedEvent(user, target, component),
                BroadcastCancelledEvent = new GuardianCreatorInjectCancelledEvent(target, component),
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
            });
        }

        private void OnCreatorInject(GuardianCreatorInjectedEvent ev)
        {
            var comp = ev.Component;

            if (comp.Deleted ||
                comp.Used ||
                !_handsSystem.IsHolding(ev.User, comp.Owner, out _) ||
                HasComp<GuardianHostComponent>(ev.Target))
            {
                comp.Injecting = false;
                return;
            }

            var hostXform = EntityManager.GetComponent<TransformComponent>(ev.Target);
            var host = EntityManager.EnsureComponent<GuardianHostComponent>(ev.Target);
            // Use map position so it's not inadvertantly parented to the host + if it's in a container it spawns outside I guess.
            var guardian = EntityManager.SpawnEntity(comp.GuardianProto, hostXform.MapPosition);

            host.GuardianContainer.Insert(guardian);
            host.HostedGuardian = guardian;

            if (TryComp(guardian, out GuardianComponent? guardianComponent))
            {
                guardianComponent.Host = ev.Target;

                SoundSystem.Play("/Audio/Effects/guardian_inject.ogg", Filter.Pvs(ev.Target), ev.Target);

                _popupSystem.PopupEntity(Loc.GetString("guardian-created"), ev.Target, ev.Target);
                // Exhaust the activator
                comp.Used = true;
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
            if (component.HostedGuardian == null) return;

            if (args.CurrentMobState.IsCritical())
            {
                _popupSystem.PopupEntity(Loc.GetString("guardian-critical-warn"), component.HostedGuardian.Value, component.HostedGuardian.Value);
                SoundSystem.Play("/Audio/Effects/guardian_warn.ogg", Filter.Pvs(component.HostedGuardian.Value), component.HostedGuardian.Value);
            }
            else if (args.CurrentMobState.IsDead())
            {
                SoundSystem.Play("/Audio/Voice/Human/malescream_guardian.ogg", Filter.Pvs(uid), uid, AudioHelpers.WithVariation(0.20f));
                EntityManager.RemoveComponent<GuardianHostComponent>(uid);
            }
        }

        /// <summary>
        /// Handles guardian receiving damage and splitting it with the host according to his defence percent
        /// </summary>
        private void OnGuardianDamaged(EntityUid uid, GuardianComponent component, DamageChangedEvent args)
        {
            if (args.DamageDelta == null) return;

            _damageSystem.TryChangeDamage(component.Host, args.DamageDelta * component.DamageShare, origin: args.Origin);
            _popupSystem.PopupEntity(Loc.GetString("guardian-entity-taking-damage"), component.Host, component.Host);

        }

        /// <summary>
        /// Triggers while trying to examine an activator to see if it's used
        /// </summary>
        private void OnCreatorExamine(EntityUid uid, GuardianCreatorComponent component, ExaminedEvent args)
        {
           if (component.Used)
           {
               args.PushMarkup(Loc.GetString("guardian-activator-empty-examine"));
           }
        }

        /// <summary>
        /// Called every time the host moves, to make sure the distance between the host and the guardian isn't too far
        /// </summary>
        private void OnHostMove(EntityUid uid, GuardianHostComponent component, ref MoveEvent args)
        {
            if (component.HostedGuardian == null ||
                !TryComp(component.HostedGuardian, out GuardianComponent? guardianComponent) ||
                !guardianComponent.GuardianLoose) return;

            CheckGuardianMove(uid, component.HostedGuardian.Value, component);
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
            if (!Resolve(hostUid, ref hostComponent, ref hostXform) ||
                !Resolve(guardianUid, ref guardianComponent, ref guardianXform))
            {
                return;
            }

            if (!guardianComponent.GuardianLoose) return;

            if (!guardianXform.Coordinates.InRange(EntityManager, hostXform.Coordinates, guardianComponent.DistanceAllowed))
            {
                RetractGuardian(hostComponent, guardianComponent);
            }
        }

        private bool CanRelease(GuardianHostComponent host, GuardianComponent guardian)
        {
            return HasComp<ActorComponent>(guardian.Owner);
        }

        private void ReleaseGuardian(GuardianHostComponent hostComponent, GuardianComponent guardianComponent)
        {
            if (guardianComponent.GuardianLoose)
            {
                DebugTools.Assert(!hostComponent.GuardianContainer.Contains(guardianComponent.Owner));
                return;
            }

            if (!CanRelease(hostComponent, guardianComponent))
            {
                _popupSystem.PopupEntity(Loc.GetString("guardian-no-soul"), hostComponent.Owner, hostComponent.Owner);
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
            _popupSystem.PopupEntity(Loc.GetString("guardian-entity-recall"), hostComponent.Owner);
            guardianComponent.GuardianLoose = false;
        }

        private sealed class GuardianCreatorInjectedEvent : EntityEventArgs
        {
            public EntityUid User { get; }
            public EntityUid Target { get; }
            public GuardianCreatorComponent Component { get; }

            public GuardianCreatorInjectedEvent(EntityUid user, EntityUid target, GuardianCreatorComponent component)
            {
                User = user;
                Target = target;
                Component = component;
            }
        }

        private sealed class GuardianCreatorInjectCancelledEvent : EntityEventArgs
        {
            public EntityUid Target { get; }
            public GuardianCreatorComponent Component { get; }

            public GuardianCreatorInjectCancelledEvent(EntityUid target, GuardianCreatorComponent component)
            {
                Target = target;
                Component = component;
            }
        }
    }
}
