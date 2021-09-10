using System;
using Content.Shared.Storage.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Storage.Events
{
    [Serializable, NetSerializable]
    public class OpenCloseBagEvent : EntityEventArgs
    {
        public readonly SharedBagState State;
        public readonly EntityUid Owner;

        public OpenCloseBagEvent(EntityUid owner, SharedBagState state)
        {
            State = state;
            Owner = owner;
        }

        public bool IsClosed => State == SharedBagState.Close;
    }
}
