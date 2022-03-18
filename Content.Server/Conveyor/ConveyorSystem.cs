using System;
using System.Collections.Generic;
using Content.Server.MachineLinking.Events;
using Content.Server.MachineLinking.Models;
using Content.Server.MachineLinking.Exceptions;
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

        }

        private void OnPortDisconnected(EntityUid uid, ConveyorComponent component, PortDisconnectedEvent args)
        {

        }

        private void OnSignalReceived(EntityUid uid, ConveyorComponent component, SignalReceivedEvent args)
        {
            switch (args.Port)
            {
                case "Forward": SetState(component, ConveyorState.Forward); break;
                case "Reverse": SetState(component, ConveyorState.Reversed); break;
                case "Off": SetState(component, ConveyorState.Off); break;
            }
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
