#nullable enable
using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.IoC;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Doors
{
    /// <summary>
    ///     Used for clientside "prediction" of door opens, preventing jarring mispredicts. Bare-bones.
    ///     Most actual behavior is handled serverside, by <see cref="ServerDoorComponent"/> The server tells
    ///     the client that the door is opening in N seconds, and the client turns the door's collision off in N seconds. 
    /// </summary>
    public abstract class SharedDoorComponent : Component
    {
        //        public override string Name => "No idea.";
        public override uint? NetID => ContentNetIDs.DOOR;

        [ViewVariables]
        protected DoorState _state = DoorState.Closed;

        // closing time until impassable
        protected TimeSpan CloseTimeOne = TimeSpan.FromSeconds(0.6f);
        // closing time until fully open
        protected TimeSpan CloseTimeTwo = TimeSpan.FromSeconds(0.3f);
        // opening time until passable
        protected TimeSpan OpenTimeOne = TimeSpan.FromSeconds(0.6f);
        // opening time until fully open
        protected TimeSpan OpenTimeTwo = TimeSpan.FromSeconds(0.3f);
        protected static TimeSpan DenyTime => TimeSpan.FromSeconds(0.45f);

        [ViewVariables(VVAccess.ReadWrite)] private bool _occludes;
        public bool Occludes => _occludes;

        /// <summary>
        /// Used by <see cref="ServerDoorComponent"/> to get the CurTime for the client to use to synchronize a "timer" with, and by <see cref="ClientDoorComponent"/> to know the CurTime to correctly use the "timer".
        /// </summary>
        [Dependency] protected IGameTiming GameTiming = default!;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);


            serializer.DataField(ref _occludes, "occludes", true);

            serializer.DataReadWriteFunction(
                "CloseTimeOne",
                0.6f,
                seconds => CloseTimeOne = TimeSpan.FromSeconds(seconds),
                () => CloseTimeOne.TotalSeconds);

            serializer.DataReadWriteFunction(
                "CloseTimeTwo",
                0.3f,
                seconds => CloseTimeTwo = TimeSpan.FromSeconds(seconds),
                () => CloseTimeOne.TotalSeconds);

            serializer.DataReadWriteFunction(
                "OpenTimeOne",
                0.6f,
                seconds => OpenTimeOne = TimeSpan.FromSeconds(seconds),
                () => CloseTimeOne.TotalSeconds);

            serializer.DataReadWriteFunction(
                "OpenTimeTwo",
                0.3f,
                seconds => OpenTimeTwo = TimeSpan.FromSeconds(seconds),
                () => CloseTimeOne.TotalSeconds);
        }

        protected virtual void OnStartOpen()
        {
            if (Occludes && Owner.TryGetComponent(out OccluderComponent? occluder))
            {
                occluder.Enabled = false;
            }
        }

        protected virtual void OnPartialOpen()
        {
            if (Owner.TryGetComponent(out IPhysicsComponent? physics))
            {
                physics.CanCollide = false;
            }
        }

        protected virtual void OnPartialClose()
        {
            if (Owner.TryGetComponent(out IPhysicsComponent? physics))
            {
                physics.CanCollide = true;
            }
        }

        protected virtual void OnFullClose()
        {
            if (_occludes && Owner.TryGetComponent(out OccluderComponent? occluder))
            {
                occluder.Enabled = true;
            }
        }

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
        Welded,
        // used to force-stop the closing animation if necessary
        EndAnimations
    }
    public class ClientDoorComponent : SharedDoorComponent
    {
        public override string Name => "Blagh.";






    }

    [Serializable, NetSerializable]
    public class DoorComponentState : ComponentState
    {
        public readonly SharedDoorComponent.DoorState DoorState;
        public readonly TimeSpan? StartTime;
        public readonly TimeSpan? Duration;

        public DoorComponentState(uint netID, SharedDoorComponent.DoorState doorState, TimeSpan? startTime, TimeSpan? duration) : base(netID)
        {
            DoorState = doorState;
            StartTime = startTime;
            Duration = duration;
        }
    }

    [NetSerializable]
    [Serializable]
    public enum DoorVisuals
    {
        VisualState,
        Powered,
        BoltLights
    }
}
