#nullable enable
using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Mobs.State;
using Content.Shared.GameObjects.Components.Storage;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Disposal
{
    public abstract class SharedDisposalUnitComponent : Component, ICollideSpecial, IDragDropOn
    {
        public override string Name => "DisposalUnit";

        private readonly List<IEntity> _intersecting = new();

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

        bool ICollideSpecial.PreventCollide(IPhysBody collided)
        {
            if (IsExiting(collided.Owner)) return true;
            if (!Owner.TryGetComponent(out IContainerManager? manager)) return false;

            if (manager.ContainsEntity(collided.Owner))
            {
                if (!_intersecting.Contains(collided.Owner))
                {
                    _intersecting.Add(collided.Owner);
                }
                return true;
            }
            return false;
        }

        public virtual void Update(float frameTime)
        {
            UpdateIntersecting();
        }

        private bool IsExiting(IEntity entity)
        {
            return _intersecting.Contains(entity);
        }

        private void UpdateIntersecting()
        {
            if(_intersecting.Count == 0) return;

            // TODO: Yeah look this sucks but we'll fix it someday.
            for (var i = _intersecting.Count - 1; i >= 0; i--)
            {
                var entity = _intersecting[i];

                if (IoCManager.Resolve<IEntityLookup>().IsIntersecting(entity, Owner))
                    _intersecting.RemoveAt(i);
            }
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
                !entity.HasComponent<IBody>())
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

        public virtual bool CanDragDropOn(DragDropEventArgs eventArgs)
        {
            return CanInsert(eventArgs.Dragged);
        }

        public abstract bool DragDropOn(DragDropEventArgs eventArgs);
    }
}
