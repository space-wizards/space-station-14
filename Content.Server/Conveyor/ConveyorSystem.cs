using Content.Server.MachineLinking.Events;
using Content.Server.MachineLinking.System;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Recycling;
using Content.Server.Recycling.Components;
using Content.Shared.Conveyor;
using Content.Shared.Item;

namespace Content.Server.Conveyor
{
    public sealed class ConveyorSystem : EntitySystem
    {
        [Dependency] private RecyclerSystem _recycler = default!;
        [Dependency] private readonly SignalLinkerSystem _signalSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ConveyorComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<ConveyorComponent, SignalReceivedEvent>(OnSignalReceived);
            SubscribeLocalEvent<ConveyorComponent, PowerChangedEvent>(OnPowerChanged);
        }

        private void OnInit(EntityUid uid, ConveyorComponent component, ComponentInit args)
        {
            _signalSystem.EnsureReceiverPorts(uid, component.ReversePort, component.ForwardPort, component.OffPort);
        }

        private void OnPowerChanged(EntityUid uid, ConveyorComponent component, PowerChangedEvent args)
        {
            UpdateAppearance(component);
        }

        private void UpdateAppearance(ConveyorComponent component)
        {
            if (!EntityManager.TryGetComponent<AppearanceComponent?>(component.Owner, out var appearance)) return;
            var isPowered = this.IsPowered(component.Owner, EntityManager);
            appearance.SetData(ConveyorVisuals.State, isPowered ? component.State : ConveyorState.Off);
        }

        private void OnSignalReceived(EntityUid uid, ConveyorComponent component, SignalReceivedEvent args)
        {
            if (args.Port == component.OffPort)
                SetState(component, ConveyorState.Off);
            else if (args.Port == component.ForwardPort)
                SetState(component, ConveyorState.Forward);
            else if (args.Port == component.ReversePort)
                SetState(component, ConveyorState.Reverse);
        }

        private void SetState(ConveyorComponent component, ConveyorState state)
        {
            component.State = state;

            if (TryComp<RecyclerComponent>(component.Owner, out var recycler))
            {
                if (component.State != ConveyorState.Off)
                    _recycler.EnableRecycler(recycler);
                else
                    _recycler.DisableRecycler(recycler);
            }

            UpdateAppearance(component);
        }

        public bool CanRun(ConveyorComponent component)
        {
            return component.State != ConveyorState.Off &&
                   !EntityManager.HasComponent<SharedItemComponent>(component.Owner) &&
                   this.IsPowered(component.Owner, EntityManager);
        }
    }
}
