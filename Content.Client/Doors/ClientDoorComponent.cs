using System;
using Content.Shared.Doors;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using DrawDepthTag = Robust.Shared.GameObjects.DrawDepth;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;


namespace Content.Client.Doors
{
    /// <summary>
    /// Bare-bones client-side door component; used to stop door-based mispredicts.
    /// </summary>
    [UsedImplicitly]
    [RegisterComponent]
    [ComponentReference(typeof(SharedDoorComponent))]
    public class ClientDoorComponent : SharedDoorComponent
    {
        [DataField("openDrawDepth", customTypeSerializer: typeof(ConstantSerializer<DrawDepthTag>))]
        public int OpenDrawDepth = (int) DrawDepth.Doors;

        [DataField("closedDrawDepth", customTypeSerializer: typeof(ConstantSerializer<DrawDepthTag>))]
        public int ClosedDrawDepth = (int) DrawDepth.Doors;

        private bool _stateChangeHasProgressed = false;
        private TimeSpan _timeOffset;

        public override DoorState State
        {
            protected set
            {
                if (State == value)
                {
                    return;
                }

                base.State = value;

                IoCManager.Resolve<IEntityManager>().EventBus.RaiseLocalEvent(Owner, new DoorStateChangedEvent(State), false);
            }
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not DoorComponentState doorCompState)
            {
                return;
            }

            CurrentlyCrushing = doorCompState.CurrentlyCrushing;
            StateChangeStartTime = doorCompState.StartTime;
            State = doorCompState.DoorState;

            if (StateChangeStartTime == null)
            {
                return;
            }

            _timeOffset = State switch
            {
                DoorState.Opening => OpenTimeOne,
                DoorState.Closing => CloseTimeOne,
                _ => throw new ArgumentOutOfRangeException(),
            };

            if (doorCompState.CurTime >= StateChangeStartTime + _timeOffset)
            {
                _stateChangeHasProgressed = true;
                return;
            }

            _stateChangeHasProgressed = false;
        }

        public void OnUpdate()
        {
            if (!_stateChangeHasProgressed)
            {
                if (GameTiming.CurTime < StateChangeStartTime + _timeOffset) return;

                if (State == DoorState.Opening)
                {
                    OnPartialOpen();
                }
                else
                {
                    OnPartialClose();
                }

                _stateChangeHasProgressed = true;
                Dirty();
            }
        }
    }
}
