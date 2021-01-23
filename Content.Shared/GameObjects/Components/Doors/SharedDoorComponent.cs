#nullable enable
using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.IoC;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using Robust.Shared.Physics;
using Robust.Shared.GameObjects.Components.Appearance;

namespace Content.Shared.GameObjects.Components.Doors
{
    /// <summary>
    ///     Used for clientside "prediction" of door opens, preventing jarring mispredicts. Bare-bones.
    ///     Most actual behavior is handled serverside, by ServerDoorComponent. The server tells
    ///     the client that the door is opening in N seconds, and the client turns the door's collision off in N seconds. 
    /// </summary>
    public abstract class SharedDoorComponent : Component, ICollideSpecial
    {
        public override string Name => "Door";
        public override uint? NetID => ContentNetIDs.DOOR;

        [ViewVariables]
        protected DoorState _state = DoorState.Closed;

        /// <summary>
        /// Closing time until impassable.
        /// </summary>
        protected TimeSpan CloseTimeOne = TimeSpan.FromSeconds(0.4f);
        /// <summary>
        /// Closing time until fully closed.
        /// </summary>
        protected TimeSpan CloseTimeTwo = TimeSpan.FromSeconds(0.2f);
        /// <summary>
        /// Opening time until passable.
        /// </summary>
        protected TimeSpan OpenTimeOne = TimeSpan.FromSeconds(0.4f);
        /// <summary>
        /// Opening time until fully open.
        /// </summary>
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

        // secret real value of CurrentlyCrushing
        private EntityUid? _currentlyCrushing = null;
        /// <summary>
        /// The EntityUid of the entity we're currently crushing, or null if we aren't crushing anyone. Reset to null in OnPartialOpen().
        /// </summary>
        protected EntityUid? CurrentlyCrushing
        {
            get => _currentlyCrushing;
            set
            {
                if (_currentlyCrushing == value)
                {
                    return;
                }
                _currentlyCrushing = value;
                Dirty();
            }
        } 

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);


            serializer.DataReadWriteFunction(
                "CloseTimeOne",
                0.4f,
                seconds => CloseTimeOne = TimeSpan.FromSeconds(seconds),
                () => CloseTimeOne.TotalSeconds);

            serializer.DataReadWriteFunction(
                "CloseTimeTwo",
                0.2f,
                seconds => CloseTimeTwo = TimeSpan.FromSeconds(seconds),
                () => CloseTimeOne.TotalSeconds);

            serializer.DataReadWriteFunction(
                "OpenTimeOne",
                0.4f,
                seconds => OpenTimeOne = TimeSpan.FromSeconds(seconds),
                () => CloseTimeOne.TotalSeconds);

            serializer.DataReadWriteFunction(
                "OpenTimeTwo",
                0.2f,
                seconds => OpenTimeTwo = TimeSpan.FromSeconds(seconds),
                () => CloseTimeOne.TotalSeconds);
        }

        protected void SetAppearance(DoorVisualState state)
        {
            if (Owner.TryGetComponent(out SharedAppearanceComponent? appearance))
            {
                appearance.SetData(DoorVisuals.VisualState, state);
            }
        }

        // stops us colliding with people we're crushing, to prevent hitbox clipping and jank
        public bool PreventCollide(IPhysBody collidedwith)
        {
            return CurrentlyCrushing == collidedwith.Entity.Uid;
        }

        /// <summary>
        /// Called when the door is partially opened.
        /// </summary>
        protected virtual void OnPartialOpen()
        {
            if (Owner.TryGetComponent(out IPhysicsComponent? physics))
            {
                physics.CanCollide = false;
            }
            // we can't be crushing anyone anymore, since we're opening
            CurrentlyCrushing = null;
        }

        /// <summary>
        /// Called when the door is partially closed.
        /// </summary>
        protected virtual void OnPartialClose()
        {
            if (Owner.TryGetComponent(out IPhysicsComponent? physics))
            {
                physics.CanCollide = true;
            }
        }

        public virtual void OnUpdate(float frameTime) { }

        // KEEP THIS IN SYNC WITH THE METHOD DAMMIT
        [NetSerializable]
        [Serializable]
        public enum DoorState
        {
            Open,
            Closed,
            Opening,
            Closing,
        }

        // KEEP THIS IN SYNC WITH THE ENUMS DAMMIT
        public static DoorVisualState DoorStateToDoorVisualState(DoorState doorState)
        {
            return doorState switch
            {
                DoorState.Open => DoorVisualState.Open,
                DoorState.Closed => DoorVisualState.Closed,
                DoorState.Opening => DoorVisualState.Opening,
                DoorState.Closing => DoorVisualState.Closing,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }
    }

    // KEEP THIS IN SYNC WITH THE METHOD DAMMIT
    [NetSerializable]
    [Serializable]
    public enum DoorVisualState
    {
        Open,
        Closed,
        Opening,
        Closing,
        Deny,
        Welded
    }

    [NetSerializable]
    [Serializable]
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
        public readonly TimeSpan CurTime;
        public readonly EntityUid? CurrentlyCrushing;

        public DoorComponentState(SharedDoorComponent.DoorState doorState, TimeSpan? startTime, TimeSpan curTime, EntityUid? currentlyCrushing) : base(ContentNetIDs.DOOR)
        {
            DoorState = doorState;
            StartTime = startTime;
            CurTime = curTime;
            CurrentlyCrushing = currentlyCrushing;
        }
    }

    public sealed class DoorStateMessage : EntitySystemMessage
    {
        public SharedDoorComponent Component { get; }
        public SharedDoorComponent.DoorState State { get; }

        public DoorStateMessage(SharedDoorComponent component, SharedDoorComponent.DoorState state)
        {
            Component = component;
            State = state;
        }
    }
}
