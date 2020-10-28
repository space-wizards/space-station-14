using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.EntitySystemMessages
{
    [Serializable, NetSerializable]
    public sealed class WeightlessChangeMessage : EntitySystemMessage
    {
        public readonly IEntity Entity;
        public readonly bool Weightless;

        public WeightlessChangeMessage(IEntity ent, bool isNowWeightless)
        {
            Entity = ent;
            Weightless = isNowWeightless;
        }
    }

}
