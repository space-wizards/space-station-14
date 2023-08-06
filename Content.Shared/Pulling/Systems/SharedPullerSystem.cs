using Content.Shared.Administration.Logs;
using Content.Shared.Alert;
using Content.Shared.Database;
using Content.Shared.Hands;
using Content.Shared.Movement.Systems;
using Content.Shared.Physics.Pull;
using Content.Shared.Pulling.Components;
using JetBrains.Annotations;

namespace Content.Shared.Pulling.Systems
{
    [UsedImplicitly]
    public sealed class SharedPullerSystem : EntitySystem
    {
        [Dependency] private readonly SharedPullingStateManagementSystem _why = default!;
        [Dependency] private readonly SharedPullingSystem _pullSystem = default!;
        [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;
        [Dependency] private readonly AlertsSystem _alertsSystem = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedPullerComponent, PullStartedMessage>(PullerHandlePullStarted);
            SubscribeLocalEvent<SharedPullerComponent, PullStoppedMessage>(PullerHandlePullStopped);
            SubscribeLocalEvent<SharedPullerComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
            SubscribeLocalEvent<SharedPullerComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
            SubscribeLocalEvent<SharedPullerComponent, ComponentShutdown>(OnPullerShutdown);
        }

        private void OnPullerShutdown(EntityUid uid, SharedPullerComponent component, ComponentShutdown args)
        {
            _why.ForceDisconnectPuller(component);
        }

        private void OnVirtualItemDeleted(EntityUid uid, SharedPullerComponent component, VirtualItemDeletedEvent args)
        {
            if (component.Pulling == null)
                return;

            if (component.Pulling == args.BlockingEntity)
            {
                if (EntityManager.TryGetComponent<SharedPullableComponent>(args.BlockingEntity, out var comp))
                {
                    _pullSystem.TryStopPull(comp, uid);
                }
            }
        }

        private void PullerHandlePullStarted(
            EntityUid uid,
            SharedPullerComponent component,
            PullStartedMessage args)
        {
            if (args.Puller.Owner != uid)
                return;

            _alertsSystem.ShowAlert(component.Owner, AlertType.Pulling);

            RefreshMovementSpeed(component);
        }

        private void PullerHandlePullStopped(
            EntityUid uid,
            SharedPullerComponent component,
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

        private void OnRefreshMovespeed(EntityUid uid, SharedPullerComponent component, RefreshMovementSpeedModifiersEvent args)
        {
            args.ModifySpeed(component.WalkSpeedModifier, component.SprintSpeedModifier);
        }

        private void RefreshMovementSpeed(SharedPullerComponent component)
        {
            _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(component.Owner);
        }
    }
}
