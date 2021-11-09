using System;
using System.Collections.Generic;
using Content.Server.Items;
using Content.Server.MachineLinking.Events;
using Content.Server.MachineLinking.Models;
using Content.Server.Power.Components;
using Content.Server.Stunnable;
using Content.Server.Stunnable.Components;
using Content.Shared.Conveyor;
using Content.Shared.MachineLinking;
using Content.Shared.Movement.Components;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Content.Server.Conveyor
{
    public class ConveyorSystem : EntitySystem
    {
        [Dependency] private StunSystem _stunSystem = default!;
        [Dependency] private IEntityLookup _entityLookup = default!;

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
            if (component.Owner.TryGetComponent<AppearanceComponent>(out var appearance))
            {
                if (component.Owner.TryGetComponent<ApcPowerReceiverComponent>(out var receiver) && receiver.Powered)
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
                _stunSystem.TryParalyze(uid, TimeSpan.FromSeconds(2f));
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
            UpdateAppearance(component);
        }

        public bool CanRun(ConveyorComponent component)
        {
            if (component.State == ConveyorState.Off)
            {
                return false;
            }

            if (component.Owner.TryGetComponent(out ApcPowerReceiverComponent? receiver) &&
                !receiver.Powered)
            {
                return false;
            }

            if (component.Owner.HasComponent<ItemComponent>())
            {
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Calculates the angle in which entities on top of this conveyor
        ///     belt are pushed in
        /// </summary>
        /// <returns>
        ///     The angle when taking into account if the conveyor is reversed
        /// </returns>
        public Angle GetAngle(ConveyorComponent component)
        {
            var adjustment = component.State == ConveyorState.Reversed ? MathHelper.Pi/2 : -MathHelper.Pi/2;
            var radians = MathHelper.DegreesToRadians(component.Angle);

            return new Angle(component.Owner.Transform.LocalRotation.Theta + radians + adjustment);
        }

        public IEnumerable<(IEntity, IPhysBody)> GetEntitiesToMove(ConveyorComponent comp)
        {
            //todo uuuhhh cache this
            foreach (var entity in _entityLookup.GetEntitiesIntersecting(comp.Owner, flags: LookupFlags.Approximate))
            {
                if (entity.Deleted)
                {
                    continue;
                }

                if (entity == comp.Owner)
                {
                    continue;
                }

                if (!entity.TryGetComponent(out IPhysBody? physics) ||
                    physics.BodyType == BodyType.Static || physics.BodyStatus == BodyStatus.InAir || entity.IsWeightless())
                {
                    continue;
                }

                if (entity.HasComponent<IMapGridComponent>())
                {
                    continue;
                }

                if (entity.IsInContainer())
                {
                    continue;
                }

                yield return (entity, physics);
            }
        }
    }
}
