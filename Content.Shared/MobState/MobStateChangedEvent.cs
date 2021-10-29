using Content.Shared.MobState.State;
using Robust.Shared.GameObjects;

namespace Content.Shared.MobState
{
    public class MobStateChangedEvent : EntityEventArgs
    {
        public IMobState NewState { get; }
        public EntityUid Uid { get; }

        public MobStateChangedEvent(IMobState newState, EntityUid uid)
        {
            NewState = newState;
            Uid = uid;
        }
    }
}
