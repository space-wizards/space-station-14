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

        private void OnPullerShutdown(Entity<SharedPullerComponent> entity, ref ComponentShutdown args)
        {
            _why.ForceDisconnectPuller(entity, entity.Comp);
        }

        private void OnVirtualItemDeleted(Entity<SharedPullerComponent> entity, ref VirtualItemDeletedEvent args)
        {
            if (entity.Comp.Pulling == null)
                return;

            if (entity.Comp.Pulling == args.BlockingEntity)
            {
                if (TryComp<SharedPullableComponent>(args.BlockingEntity, out var comp))
                {
                    _pullSystem.TryStopPull((args.BlockingEntity, comp), entity);
                }
            }
        }

        private void PullerHandlePullStarted(Entity<SharedPullerComponent> entity, ref PullStartedMessage args)
        {
            if (args.Puller != entity.Owner)
                return;

            _alertsSystem.ShowAlert(entity, AlertType.Pulling);

            RefreshMovementSpeed(entity);
        }

        private void PullerHandlePullStopped(Entity<SharedPullerComponent> entity, ref PullStoppedMessage args)
        {
            if (args.Puller != entity.Owner)
                return;

            if (_alertsSystem.IsShowingAlert(entity, AlertType.Pulling))
                _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(entity):user} stopped pulling {ToPrettyString(args.Pulled):target}");
            _alertsSystem.ClearAlert(entity, AlertType.Pulling);

            RefreshMovementSpeed(entity);
        }

        private void OnRefreshMovespeed(Entity<SharedPullerComponent> entity, ref RefreshMovementSpeedModifiersEvent args)
        {
            args.ModifySpeed(entity.Comp.WalkSpeedModifier, entity.Comp.SprintSpeedModifier);
        }

        private void RefreshMovementSpeed(Entity<SharedPullerComponent> entity)
        {
            _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(entity);
        }
    }
}
