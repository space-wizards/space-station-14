using Content.Shared.Popups;
using Content.Shared.Actions;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Gibbing;
using Content.Shared.Guardian.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mech.EntitySystems;
using Content.Shared.Mobs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared.Guardian
{
    /// <summary>
    /// A guardian has a host it's attached to that it fights for. A fighting spirit.
    /// </summary>
    public sealed class GuardianSystem : EntitySystem
    {
        [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = null!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = null!;
        [Dependency] private readonly DamageableSystem _damageSystem = null!;
        [Dependency] private readonly SharedActionsSystem _actionSystem = null!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = null!;
        [Dependency] private readonly SharedAudioSystem _audio = null!;
        [Dependency] private readonly GibbingSystem _gibbing = null!;
        [Dependency] private readonly SharedContainerSystem _container = null!;
        [Dependency] private readonly SharedTransformSystem _transform = null!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<GuardianCreatorComponent, UseInHandEvent>(OnCreatorUse);
            SubscribeLocalEvent<GuardianCreatorComponent, AfterInteractEvent>(OnCreatorInteract);
            SubscribeLocalEvent<GuardianCreatorComponent, ExaminedEvent>(OnCreatorExamine);
            SubscribeLocalEvent<GuardianCreatorComponent, GuardianCreatorDoAfterEvent>(OnDoAfter);

            SubscribeLocalEvent<GuardianComponent, ComponentShutdown>(OnGuardianShutdown);
            SubscribeLocalEvent<GuardianComponent, MoveEvent>(OnGuardianMove);
            SubscribeLocalEvent<GuardianComponent, DamageChangedEvent>(OnGuardianDamaged);
            SubscribeLocalEvent<GuardianComponent, PlayerAttachedEvent>(OnGuardianPlayerAttached);
            SubscribeLocalEvent<GuardianComponent, PlayerDetachedEvent>(OnGuardianPlayerDetached);

            SubscribeLocalEvent<GuardianHostComponent, ComponentInit>(OnHostInit);
            SubscribeLocalEvent<GuardianHostComponent, MoveEvent>(OnHostMove);
            SubscribeLocalEvent<GuardianHostComponent, MobStateChangedEvent>(OnHostStateChange);
            SubscribeLocalEvent<GuardianHostComponent, ComponentShutdown>(OnHostShutdown);

            SubscribeLocalEvent<GuardianHostComponent, GuardianToggleActionEvent>(OnPerformAction);

            SubscribeLocalEvent<GuardianComponent, AttackAttemptEvent>(OnGuardianAttackAttempt);

            SubscribeLocalEvent<GuardianHostComponent, MechPilotRelayedEvent<GettingAttackedAttemptEvent>>(OnPilotAttackAttempt);
        }

        private void OnGuardianShutdown(Entity<GuardianComponent> ent, ref ComponentShutdown args)
        {
            ent.Comp = null!;

            if (!TryComp(ent, out GuardianHostComponent? hostComponent))
                return;

            _container.Remove(ent.Owner, hostComponent.GuardianContainer);
            hostComponent.HostedGuardian = null!;
            PredictedDel(hostComponent.ActionEntity);
            hostComponent.ActionEntity = null;

            Dirty(ent);
        }

        private void OnPerformAction(Entity<GuardianHostComponent> ent, ref GuardianToggleActionEvent args)
        {
            if (args.Handled)
                return;

            if (_container.IsEntityInContainer(ent.Owner))
            {
                _popupSystem.PopupPredicted(Loc.GetString("guardian-inside-container"), ent.Owner, ent.Owner);
                return;
            }

            if (ent.Comp.HostedGuardian != null)
                ToggleGuardian(ent);
            args.Handled = true;
        }

        private void OnGuardianPlayerDetached(Entity<GuardianComponent> ent, ref PlayerDetachedEvent args)
        {
            if (!TryComp<GuardianHostComponent>(ent.Comp.Host, out var hostComponent) ||
                TerminatingOrDeleted(ent.Owner))
            {
                PredictedDel(ent.Owner);
                return;
            }
            Dirty(ent);
            RetractGuardian((ent.Comp.Host.Value, hostComponent), (ent.Owner, ent.Comp));
        }

        private void OnGuardianPlayerAttached(Entity<GuardianComponent> ent, ref PlayerAttachedEvent args)
        {
            var host = ent.Comp.Host;
            Dirty(ent);
            if (!HasComp<GuardianHostComponent>(host))
            {
                PredictedDel(ent.Owner);
                return;
            }

            _popupSystem.PopupPredicted(Loc.GetString("guardian-available"), host.Value, host.Value);
        }

        private void OnHostInit(Entity<GuardianHostComponent> ent, ref ComponentInit args)
        {
            ent.Comp.GuardianContainer = _container.EnsureContainer<ContainerSlot>(ent.Owner, "GuardianContainer");
            _actionSystem.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.Action);
        }

        private void OnHostShutdown(Entity<GuardianHostComponent> ent, ref ComponentShutdown args)
        {
            if (ent.Comp.HostedGuardian is not {} guardian)
                return;

            // Ensure held items are dropped before deleting guardian.
            if (HasComp<HandsComponent>(guardian))
                _gibbing.Gib(ent.Comp.HostedGuardian.Value);

            PredictedDel(guardian);
            PredictedDel(ent.Comp.ActionEntity);
            ent.Comp.ActionEntity = null;
        }

        private void OnGuardianAttackAttempt(Entity<GuardianComponent> ent, ref AttackAttemptEvent args)
        {
            if (args.Cancelled || args.Target != ent.Comp.Host)
                return;

            // it's predicted now!
            _popupSystem.PopupPredictedCursor(Loc.GetString("guardian-attack-host"), ent.Owner, PopupType.LargeCaution);
            args.Cancel();
        }

        private void OnPilotAttackAttempt(Entity<GuardianHostComponent> ent, ref MechPilotRelayedEvent<GettingAttackedAttemptEvent> args)
        {
            if (args.Args.Cancelled)
                return;

            _popupSystem.PopupPredictedCursor(Loc.GetString("guardian-attack-host"), args.Args.Attacker, PopupType.LargeCaution);
            args.Args.Cancelled = true;
        }

        private void ToggleGuardian(Entity<GuardianHostComponent> ent)
        {

            if (!TryComp<GuardianComponent>(ent.Comp.HostedGuardian, out var guardianComponent))
                return;

            if (guardianComponent.GuardianLoose)
                RetractGuardian(ent, (ent.Comp.HostedGuardian.Value, guardianComponent));
            else
                ReleaseGuardian(ent, (ent.Comp.HostedGuardian.Value, guardianComponent));
        }

        /// <summary>
        /// Adds the guardian host component to the user and spawns the guardian inside said component
        /// </summary>
        private void OnCreatorUse(Entity<GuardianCreatorComponent> ent, ref UseInHandEvent args)
        {
            if (args.Handled)
                return;

            args.Handled = true;
            UseCreator(args.User, args.User, ent);
            Dirty(ent);
        }

        private void OnCreatorInteract(Entity<GuardianCreatorComponent> ent, ref AfterInteractEvent args)
        {
            if (args.Handled || args.Target == null || !args.CanReach)
                return;

            args.Handled = true;
            UseCreator(args.User, args.Target.Value, ent);
        }
        private void UseCreator(EntityUid user, EntityUid target, Entity<GuardianCreatorComponent> ent)
        {
            if (ent.Comp.Used)
            {
                _popupSystem.PopupPredicted(Loc.GetString("guardian-activator-empty-invalid-creation"), user, user);
                return;
            }

            // Can only inject things with the component...
            if (!HasComp<CanHostGuardianComponent>(target))
            {
                var msg = Loc.GetString("guardian-activator-invalid-target", ("entity", Identity.Entity(target, EntityManager, user)));
                _popupSystem.PopupPredicted(msg, user, user);
                return;
            }

            // If user is already a host don't duplicate.
            if (HasComp<GuardianHostComponent>(target))
            {
                _popupSystem.PopupPredicted(Loc.GetString("guardian-already-present-invalid-creation"), user, user);
                return;
            }

            _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, user, ent.Comp.InjectionDelay, new GuardianCreatorDoAfterEvent(), ent, target: target, used: ent)
            {
                BreakOnMove = true,
                NeedHand = true,
                BreakOnHandChange = true
            });
        }

        private void OnDoAfter(Entity<GuardianCreatorComponent> ent, ref GuardianCreatorDoAfterEvent args)
        {
            if (args.Handled || args.Args.Target == null)
                return;

            if (args.Cancelled || ent.Comp.Deleted || ent.Comp.Used || !_handsSystem.IsHolding(args.Args.User, ent.Owner, out _) || HasComp<GuardianHostComponent>(args.Args.Target))
                return;

            var hostXform = Transform(args.Args.Target.Value);
            var host = EnsureComp<GuardianHostComponent>(args.Args.Target.Value);
            // Use map position so it's not inadvertently parented to the host + if it's in a container it spawns outside I guess.
            var guardian = Spawn(ent.Comp.GuardianProto, _transform.GetMapCoordinates(args.Args.Target.Value, xform: hostXform));

            _container.Insert(guardian, host.GuardianContainer);
            host.HostedGuardian = guardian;

            if (TryComp<GuardianComponent>(guardian, out var guardianComp))
            {
                guardianComp.Host = args.Args.Target.Value;
                _audio.PlayPredicted((!ent.Comp.Magical ? guardianComp.InjectSound : guardianComp.DeckSound), ent.Owner, args.Args.Target);
                _popupSystem.PopupClient(Loc.GetString("guardian-created"), args.Args.Target.Value, args.Args.Target.Value);
                // Exhaust the activator
                ent.Comp.Used = true;
            }
            else
            {
                Log.Error($"Tried to spawn a guardian that doesn't have {nameof(GuardianComponent)}");
                PredictedDel(guardian);
            }
            Dirty(ent);
            args.Handled = true;
        }

        /// <summary>
        /// Triggers when the host receives damage which puts the host in either critical or killed state
        /// </summary>
        private void OnHostStateChange(Entity<GuardianHostComponent> ent, ref MobStateChangedEvent args)
        {
            if (ent.Comp.HostedGuardian == null)
                return;

            TryComp<GuardianComponent>(ent.Comp.HostedGuardian, out var guardianComp);

            if (args.NewMobState == MobState.Critical)
            {
                _popupSystem.PopupClient(Loc.GetString("guardian-host-critical-warn"), ent.Comp.HostedGuardian.Value, ent.Comp.HostedGuardian.Value);
                if (guardianComp != null)
                    _audio.PlayPredicted(guardianComp.CriticalSound, ent.Comp.HostedGuardian.Value, args.Target);
            }
            else if (args.NewMobState == MobState.Dead)
            {
                if (guardianComp != null)
                    _audio.PlayPredicted(guardianComp.DeathSound, ent.Owner, args.Target);
                RemComp<GuardianHostComponent>(ent.Owner);
            }
            Dirty(ent);
        }

        /// <summary>
        /// Handles guardian receiving damage and splitting it with the host according to his defense percent
        /// </summary>
        private void OnGuardianDamaged(Entity<GuardianComponent> ent, ref DamageChangedEvent args)
        {
            if (args.DamageDelta == null || ent.Comp.Host == null || ent.Comp.DamageShare == 0)
                return;

            _damageSystem.ChangeDamage(
                ent.Comp.Host.Value,
                args.DamageDelta * ent.Comp.DamageShare,
                origin: args.Origin,
                ignoreResistances: true,
                interruptsDoAfters: false);
            _popupSystem.PopupClient(Loc.GetString("guardian-entity-taking-damage"), ent.Comp.Host.Value, ent.Comp.Host.Value);
            Dirty(ent);
        }

        /// <summary>
        /// Triggers while trying to examine an activator to see if it's used
        /// </summary>
        private void OnCreatorExamine(Entity<GuardianCreatorComponent> ent, ref ExaminedEvent args)
        {
           if (ent.Comp.Used & !ent.Comp.Magical)
               args.PushMarkup(Loc.GetString("guardian-activator-empty-examine"));
           else
               args.PushMarkup(Loc.GetString("guardian-wizard-activator-empty-examine"));
           Dirty(ent);
        }

        /// <summary>
        /// Called every time the host moves, to make sure the distance between the host and the guardian are not too far
        /// </summary>
        private void OnHostMove(Entity<GuardianHostComponent> ent, ref MoveEvent args)
        {
            if (!TryComp(ent.Comp.HostedGuardian, out GuardianComponent? guardianComponent) ||
                !guardianComponent.GuardianLoose)
            {
                return;
            }
            CheckGuardianMove(ent.Owner, ent.Comp.HostedGuardian.Value);
            Dirty(ent);
        }

        /// <summary>
        /// Called every time the guardian moves: makes sure it's not out of it's allowed distance
        /// </summary>
        private void OnGuardianMove(Entity<GuardianComponent> ent, ref MoveEvent args)
        {
            if (!ent.Comp.GuardianLoose)
                return;
            if (ent.Comp.Host == null)
                return;

            CheckGuardianMove(ent.Comp.Host.Value, ent.Owner);
            Dirty(ent);
        }

        /// <summary>
        /// Retract the guardian if either the host or the guardian move away from each other.
        /// </summary>
        private void CheckGuardianMove(
            Entity<GuardianHostComponent?> host,
            Entity<GuardianComponent?> guardian,
            TransformComponent? hostXform = null,
            TransformComponent? guardianXform = null)
        {
            if (TerminatingOrDeleted(guardian.Owner) || TerminatingOrDeleted(host.Owner))
                return;

            if (!Resolve(host.Owner, ref host.Comp, ref hostXform) ||
                !Resolve(guardian.Owner, ref guardian.Comp, ref guardianXform))
            {
                return;
            }

            if (!guardian.Comp.GuardianLoose)
                return;

            if (!_transform.InRange(guardianXform.Coordinates, hostXform.Coordinates, guardian.Comp.DistanceAllowed))
                RetractGuardian((host.Owner, host.Comp), guardian);
        }

        private void ReleaseGuardian(Entity<GuardianHostComponent> host, Entity<GuardianComponent> guardian)
        {
            if (guardian.Comp.GuardianLoose)
            {
                DebugTools.Assert(!host.Comp.GuardianContainer.Contains(guardian));
                return;
            }

            DebugTools.Assert(host.Comp.GuardianContainer.Contains(guardian));
            _container.Remove(guardian.Owner, host.Comp.GuardianContainer);
            DebugTools.Assert(!host.Comp.GuardianContainer.Contains(guardian));

            guardian.Comp.GuardianLoose = true;
            Dirty(host);
            Dirty(guardian);
        }

        private void RetractGuardian(Entity<GuardianHostComponent> host, Entity<GuardianComponent?> guardian)
        {
            if (!Resolve(guardian, ref guardian.Comp))
                return;

            if (!guardian.Comp.GuardianLoose)
            {
                DebugTools.Assert(host.Comp.GuardianContainer.Contains(guardian));
                return;
            }

            if (_container.ContainsEntity(host.Comp.GuardianContainer.Owner, guardian.Owner))
            {
                Log.Debug("stopped guardian from being inserted again");
                return;
            }

            Log.Debug("Inserting the guardian into the.");
            //_container.Insert(guardian.Owner, host.Comp.GuardianContainer);

            DebugTools.Assert(host.Comp.GuardianContainer.Contains(guardian));
            _popupSystem.PopupPredicted(Loc.GetString("guardian-entity-recall"), host.Owner, host.Owner);
            guardian.Comp.GuardianLoose = false;
            Dirty(host);
            Dirty(guardian);
        }
    }
}
