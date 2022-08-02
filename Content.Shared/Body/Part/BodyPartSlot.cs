using Content.Shared.Body.Components;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared.Body.Part
{
    [Serializable, NetSerializable]
    public sealed class BodyPartSlot
    {

        /// <summary>
        ///     The ID of this slot.
        /// </summary>
        [ViewVariables]
        public string Id = string.Empty;

        /// <summary>
        ///     The part type that this slot accepts.
        /// </summary>
        [ViewVariables]
        public BodyPartType PartType = BodyPartType.Other;

        [ViewVariables, NonSerialized]
        public ContainerSlot? ContainerSlot = default!;

        /// <summary>
        ///     List of slots that this slot connects to.
        /// </summary>
        [ViewVariables]
        public HashSet<string> Connections = new();

        [ViewVariables]
        public bool IsCenterSlot = false;

        public bool HasPart => ContainerSlot?.ContainedEntity != null;
        public EntityUid? Part => ContainerSlot?.ContainedEntity;

        public BodyPartSlot(string id, BodyPartType type)
        {
            Id = id;
            PartType = type;
        }

        public BodyPartSlot(BodyPartSlot slot)
        {
            Id = slot.Id;
            PartType = slot.PartType;
            Connections = slot.Connections;
            IsCenterSlot = slot.IsCenterSlot;
        }
    }
}
