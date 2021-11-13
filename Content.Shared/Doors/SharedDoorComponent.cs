using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Doors
{
    [NetworkedComponent]
    public abstract class SharedDoorComponent : Component
    {
        public override string Name => "Door";

        [ComponentDependency]
        protected readonly SharedAppearanceComponent? AppearanceComponent = null;

        [ComponentDependency]
        protected readonly IPhysBody? PhysicsComponent = null;

        [Dependency]
        protected readonly IGameTiming _gameTiming = default!;

        [ViewVariables]
        private DoorState _state = DoorState.Closed;
        /// <summary>
        /// The current state of the door -- whether it is open, closed, opening, or closing.
        /// </summary>
        public virtual DoorState State
        {
            get => _state;
            protected set
            {
                if (_state == value)
                {
                    return;
                }

                _state = value;

                SetAppearance(State switch
                {
                    DoorState.Open => DoorVisualState.Open,
                    DoorState.Closed => DoorVisualState.Closed,
                    DoorState.Opening => DoorVisualState.Opening,
                    DoorState.Closing => DoorVisualState.Closing,
                    _ => throw new ArgumentOutOfRangeException(),
                });
            }
        }

        /// <summary>
        /// Closing time until impassable.
        /// </summary>
        [DataField("closeTimeOne")]
        protected TimeSpan CloseTimeOne = TimeSpan.FromSeconds(0.4f);

        /// <summary>
        /// Closing time until fully closed.
        /// </summary>
        [DataField("closeTimeTwo")]
        protected TimeSpan CloseTimeTwo = TimeSpan.FromSeconds(0.2f);

        /// <summary>
        /// Opening time until passable.
        /// </summary>
        [DataField("openTimeOne")]
        protected TimeSpan OpenTimeOne = TimeSpan.FromSeconds(0.4f);

        /// <summary>
        /// Opening time until fully open.
        /// </summary>
        [DataField("openTimeTwo")]
        protected TimeSpan OpenTimeTwo = TimeSpan.FromSeconds(0.2f);

        /// <summary>
        /// Time to finish denying.
        /// </summary>
        protected static TimeSpan DenyTime => TimeSpan.FromSeconds(0.45f);

        /// <summary>
        /// Used by ServerDoorComponent to get the CurTime for the client to use to know when to open, and by ClientDoorComponent to know the CurTime to correctly open.
        /// </summary>
        [Dependency] protected IGameTiming GameTiming = default!;

        /// <summary>
        /// The time the door began to open or close, if the door is opening or closing, or null if it is neither.
        /// </summary>
        protected TimeSpan? StateChangeStartTime = null;

        /// <summary>
        /// List of EntityUids of entities we're currently crushing. Cleared in OnPartialOpen().
        /// </summary>
        protected List<EntityUid> CurrentlyCrushing = new();

        public bool IsCrushing(IEntity entity)
        {
            return CurrentlyCrushing.Contains(entity.Uid);
        }

        protected void SetAppearance(DoorVisualState state)
        {
            AppearanceComponent?.SetData(DoorVisuals.VisualState, state);
        }

        /// <summary>
        /// Called when the door is partially opened.
        /// </summary>
        protected virtual void OnPartialOpen()
        {
            if (PhysicsComponent != null)
            {
                PhysicsComponent.CanCollide = false;
            }
            // we can't be crushing anyone anymore, since we're opening
            CurrentlyCrushing.Clear();
        }

        /// <summary>
        /// Called when the door is partially closed.
        /// </summary>
        protected virtual void OnPartialClose()
        {
            if (PhysicsComponent != null)
            {
                PhysicsComponent.CanCollide = true;
            }
        }

        [Serializable, NetSerializable]
        public enum DoorState
        {
            Open,
            Closed,
            Opening,
            Closing,
        }

    }

    [Serializable, NetSerializable]
    public enum DoorVisualState
    {
        Open,
        Closed,
        Opening,
        Closing,
        Deny,
        Welded
    }

    [Serializable, NetSerializable]
    public enum DoorVisuals
    {
        VisualState,
        Powered,
        BoltLights
    }

    [Serializable, NetSerializable]
    public class DoorComponentState : ComponentState
    {
        public readonly SharedDoorComponent.DoorState DoorState;
        public readonly TimeSpan? StartTime;
        public readonly List<EntityUid> CurrentlyCrushing;
        public readonly TimeSpan CurTime;

        public DoorComponentState(SharedDoorComponent.DoorState doorState, TimeSpan? startTime, List<EntityUid> currentlyCrushing, TimeSpan curTime)
        {
            DoorState = doorState;
            StartTime = startTime;
            CurrentlyCrushing = currentlyCrushing;
            CurTime = curTime;
        }
    }

    public sealed class DoorOpenAttemptEvent : CancellableEntityEventArgs
    {

    }

    public sealed class DoorCloseAttemptEvent : CancellableEntityEventArgs
    {

    }
}
