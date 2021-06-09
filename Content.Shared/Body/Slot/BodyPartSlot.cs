using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Body.Part;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Body.Slot
{
    public class BodyPartSlot
    {
        public BodyPartSlot(string id, BodyPartType partType, IEnumerable<BodyPartSlot> connections)
        {
            Id = id;
            PartType = partType;
            Connections = new HashSet<BodyPartSlot>(connections);
        }

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
        public IBodyPart? Part { get; private set; }

        /// <summary>
        ///     List of slots that this slot connects to.
        /// </summary>
        [ViewVariables]
        public HashSet<BodyPartSlot> Connections { get; private set; }

        public event Action<IBodyPart>? PartAdded;

        public event Action<IBodyPart>? PartRemoved;

        internal void SetConnectionsInternal(IEnumerable<BodyPartSlot> connections)
        {
            Connections = new HashSet<BodyPartSlot>(connections);
        }

        public bool CanAddPart(IBodyPart part)
        {
            return Part == null && part.PartType == PartType;
        }

        public bool TryAddPart(IBodyPart part)
        {
            if (!CanAddPart(part))
            {
                return false;
            }

            SetPart(part);
            return true;
        }

        public void SetPart(IBodyPart part)
        {
            if (Part != null)
            {
                RemovePart();
            }

            Part = part;
            PartAdded?.Invoke(part);
        }

        public bool RemovePart()
        {
            if (Part == null)
            {
                return false;
            }

            var old = Part;
            Part = null;

            PartRemoved?.Invoke(old);

            return true;
        }

        public void Shutdown()
        {
            Part = null;
            Connections.Clear();
            PartAdded = null;
            PartRemoved = null;
        }
    }
}
