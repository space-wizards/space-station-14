using Content.Shared.Administration.Logs;
using Content.Shared.Alert;
using Content.Shared.Database;
using Content.Shared.Hands;
using Content.Shared.Movement.Systems;
using Content.Shared.Physics.Pull;
using Content.Shared.Pulling.Components;
using Content.Shared.Pulling.Events;
using JetBrains.Annotations;

namespace Content.Shared.Pulling.Systems
{
    [UsedImplicitly]
    public sealed class SharedPullerSystem : EntitySystem
    {
        [Dependency] private readonly SharedPullingStateManagementSystem _why = default!;
        [Dependency] private readonly PullingSystem _pullSystem = default!;
        [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;
        [Dependency] private readonly AlertsSystem _alertsSystem = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PullerComponent, PullStartedMessage>(PullerHandlePullStarted);
            SubscribeLocalEvent<PullerComponent, PullStoppedMessage>(PullerHandlePullStopped);
            SubscribeLocalEvent<PullerComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
            SubscribeLocalEvent<PullerComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
            SubscribeLocalEvent<PullerComponent, ComponentShutdown>(OnPullerShutdown);
        }

        private void OnPullerShutdown(EntityUid uid, PullerComponent component, ComponentShutdown args)
        {
            _why.ForceDisconnectPuller(component);
        }

        private void OnVirtualItemDeleted(EntityUid uid, PullerComponent component, VirtualItemDeletedEvent args)
        {
            if (component.Pulling == null)
                return;

            if (component.Pulling == args.BlockingEntity)
            {
                if (EntityManager.TryGetComponent<PullableComponent>(args.BlockingEntity, out var comp))
                {
                    _pullSystem.TryStopPull(comp, uid);
                }
            }
        }

        private void PullerHandlePullStarted(
            EntityUid uid,
            PullerComponent component,
            PullStartedMessage args)
        {
            if (args.Puller.Owner != uid)
                return;

            _alertsSystem.ShowAlert(component.Owner, AlertType.Pulling);

            RefreshMovementSpeed(component);
        }

        private void PullerHandlePullStopped(
            EntityUid uid,
            PullerComponent component,
            PullStoppedMessage args)
        {
            if (args.Puller.Owner != uid)
                return;

            var euid = component.Owner;
            if (_alertsSystem.IsShowingAlert(euid, AlertType.Pulling))
                _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(euid):user} stopped pulling {ToPrettyString(args.Pulled.Owner):target}");
            _alertsSystem.ClearAlert(euid, AlertType.Pulling);

            RefreshMovementSpeed(component);
        }

        private void OnRefreshMovespeed(EntityUid uid, PullerComponent component, RefreshMovementSpeedModifiersEvent args)
        {
            args.ModifySpeed(component.WalkSpeedModifier, component.SprintSpeedModifier);
        }

        private void RefreshMovementSpeed(PullerComponent component)
        {
            _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(component.Owner);
        }
    }
}
