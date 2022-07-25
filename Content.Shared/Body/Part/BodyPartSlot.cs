using Content.Shared.Body.Components;

namespace Content.Shared.Body.Part
{
    public sealed class BodyPartSlot
    {
        public BodyPartSlot(string id, BodyPartType partType)
        {
            Id = id;
            PartType = partType;
            Connections = new HashSet<BodyPartSlot>();
        }

        /// <summary>
        ///     The ID of this slot.
        /// </summary>
        [ViewVariables]
        public string Id { get; }

        /// <summary>
        ///     The part type that this slot accepts.
        /// </summary>
        [ViewVariables]
        public BodyPartType PartType { get; }

        /// <summary>
        ///     The part currently in this slot, if any.
        /// </summary>
        [ViewVariables]
        public SharedBodyPartComponent? Part { get; set; }

        /// <summary>
        ///     List of slots that this slot connects to.
        /// </summary>
        [ViewVariables]
        public HashSet<BodyPartSlot> Connections { get; set; }
    }
}
