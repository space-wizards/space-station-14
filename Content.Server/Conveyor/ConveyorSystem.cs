using System;
using System.Collections.Generic;
using Content.Server.MachineLinking.Events;
using Content.Server.MachineLinking.Models;
using Content.Server.Power.Components;
using Content.Server.Recycling;
using Content.Server.Recycling.Components;
using Content.Server.Stunnable;
using Content.Shared.Conveyor;
using Content.Shared.Item;
using Content.Shared.MachineLinking;
using Content.Shared.Movement.Components;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Content.Server.Conveyor
{
    public sealed class ConveyorSystem : EntitySystem
    {
        [Dependency] private RecyclerSystem _recycler = default!;
        [Dependency] private StunSystem _stunSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ConveyorComponent, SignalReceivedEvent>(OnSignalReceived);
            SubscribeLocalEvent<ConveyorComponent, PortDisconnectedEvent>(OnPortDisconnected);
            SubscribeLocalEvent<ConveyorComponent, LinkAttemptEvent>(OnLinkAttempt);
            SubscribeLocalEvent<ConveyorComponent, PowerChangedEvent>(OnPowerChanged);
        }

        private void OnPowerChanged(EntityUid uid, ConveyorComponent component, PowerChangedEvent args)
        {
            UpdateAppearance(component);
        }

        private void UpdateAppearance(ConveyorComponent component)
        {
            if (EntityManager.TryGetComponent<AppearanceComponent?>(component.Owner, out var appearance))
            {
                if (EntityManager.TryGetComponent<ApcPowerReceiverComponent?>(component.Owner, out var receiver) && receiver.Powered)
                {
                    appearance.SetData(ConveyorVisuals.State, component.State);
                }
                else
                {
                    appearance.SetData(ConveyorVisuals.State, ConveyorState.Off);
                }
            }
        }

        private void OnLinkAttempt(EntityUid uid, ConveyorComponent component, LinkAttemptEvent args)
        {
            if (args.TransmitterComponent.Outputs.GetPort(args.TransmitterPort).Signal is TwoWayLeverSignal signal &&
                signal != TwoWayLeverSignal.Middle)
            {
                args.Cancel();
                _stunSystem.TryParalyze(uid, TimeSpan.FromSeconds(2f), true);
                component.Owner.PopupMessage(args.Attemptee, Loc.GetString("conveyor-component-failed-link"));
            }
        }

        private void OnPortDisconnected(EntityUid uid, ConveyorComponent component, PortDisconnectedEvent args)
        {
            SetState(component, TwoWayLeverSignal.Middle);
        }

        private void OnSignalReceived(EntityUid uid, ConveyorComponent component, SignalReceivedEvent args)
        {
            switch (args.Port)
            {
                case "state":
                    SetState(component, (TwoWayLeverSignal) args.Value!);
                    break;
            }
        }

        private void SetState(ConveyorComponent component, TwoWayLeverSignal signal)
        {
            component.State = signal switch
            {
                TwoWayLeverSignal.Left => ConveyorState.Reversed,
                TwoWayLeverSignal.Middle => ConveyorState.Off,
                TwoWayLeverSignal.Right => ConveyorState.Forward,
                _ => ConveyorState.Off
            };

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
            if (component.State == ConveyorState.Off)
            {
                return false;
            }

            if (EntityManager.TryGetComponent(component.Owner, out ApcPowerReceiverComponent? receiver) &&
                !receiver.Powered)
            {
                return false;
            }

            if (EntityManager.HasComponent<SharedItemComponent>(component.Owner))
            {
                return false;
            }

            return true;
        }
    }
}
