#nullable enable
using System;
using Content.Shared.Body.Components;
using Content.Shared.DragDrop;
using Content.Shared.Item;
using Content.Shared.MobState;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Disposal.Components
{
    public abstract class SharedDisposalUnitComponent : Component, IDragDropOn
    {
        public override string Name => "DisposalUnit";

        [ViewVariables]
        public bool Anchored =>
            !Owner.TryGetComponent(out IPhysBody? physics) ||
            physics.BodyType == BodyType.Static;

        [Serializable, NetSerializable]
        public enum Visuals
        {
            VisualState,
            Handle,
            Light
        }

        [Serializable, NetSerializable]
        public enum VisualState
        {
            UnAnchored,
            Anchored,
            Flushing,
            Charging
        }

        [Serializable, NetSerializable]
        public enum HandleState
        {
            Normal,
            Engaged
        }

        [Serializable, NetSerializable]
        public enum LightState
        {
            Off,
            Charging,
            Full,
            Ready
        }

        [Serializable, NetSerializable]
        public enum UiButton
        {
            Eject,
            Engage,
            Power
        }

        [Serializable, NetSerializable]
        public enum PressureState
        {
            Ready,
            Pressurizing
        }

        public virtual void Update(float frameTime)
        {
            return;
        }

        [Serializable, NetSerializable]
        public class DisposalUnitBoundUserInterfaceState : BoundUserInterfaceState, IEquatable<DisposalUnitBoundUserInterfaceState>
        {
            public readonly string UnitName;
            public readonly string UnitState;
            public readonly float Pressure;
            public readonly bool Powered;
            public readonly bool Engaged;

            public DisposalUnitBoundUserInterfaceState(string unitName, string unitState, float pressure, bool powered,
                bool engaged)
            {
                UnitName = unitName;
                UnitState = unitState;
                Pressure = pressure;
                Powered = powered;
                Engaged = engaged;
            }

            public bool Equals(DisposalUnitBoundUserInterfaceState? other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return UnitName == other.UnitName &&
                       UnitState == other.UnitState &&
                       Powered == other.Powered &&
                       Engaged == other.Engaged &&
                       Pressure.Equals(other.Pressure);
            }
        }

        /// <summary>
        ///     Message data sent from client to server when a disposal unit ui button is pressed.
        /// </summary>
        [Serializable, NetSerializable]
        public class UiButtonPressedMessage : BoundUserInterfaceMessage
        {
            public readonly UiButton Button;

            public UiButtonPressedMessage(UiButton button)
            {
                Button = button;
            }
        }

        [Serializable, NetSerializable]
        public enum DisposalUnitUiKey
        {
            Key
        }

        public virtual bool CanInsert(IEntity entity)
        {
            if (!Anchored)
                return false;

            // TODO: Probably just need a disposable tag.
            if (!entity.TryGetComponent(out SharedItemComponent? storable) &&
                !entity.HasComponent<SharedBodyComponent>())
            {
                return false;
            }


            if (!entity.TryGetComponent(out IPhysBody? physics) ||
                !physics.CanCollide && storable == null)
            {
                if (!(entity.TryGetComponent(out IMobStateComponent? damageState) && damageState.IsDead())) {
                    return false;
                }
            }
            return true;
        }

        public virtual bool CanDragDropOn(DragDropEvent eventArgs)
        {
            return CanInsert(eventArgs.Dragged);
        }

        public abstract bool DragDropOn(DragDropEvent eventArgs);
    }
}
