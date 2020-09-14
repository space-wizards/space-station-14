#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Body
{
    public abstract class SharedBodyComponent : DamageableComponent, IBody
    {
        public override string Name => "BodyManager";

        public override uint? NetID => ContentNetIDs.BODY;

        private Dictionary<string, string> _partIds = new Dictionary<string, string>();

        [ViewVariables] public string TemplateName { get; private set; } = string.Empty;

        [ViewVariables]
        public Dictionary<string, BodyPartType> Slots { get; private set; } = new Dictionary<string, BodyPartType>();

        [ViewVariables]
        public Dictionary<string, List<string>> Connections { get; private set; } = new Dictionary<string, List<string>>();

        [ViewVariables] public IReadOnlyDictionary<string, string> PartIDs { get; protected set; }

        public bool TryAddPart(string slot, IBodyPart part, bool force = false)
        {
            throw new NotImplementedException();
        }

        public bool HasPart(string slot)
        {
            throw new NotImplementedException();
        }

        public void RemovePart(IBodyPart part, bool drop)
        {
            throw new NotImplementedException();
        }

        public bool RemovePart(string slot, bool drop)
        {
            throw new NotImplementedException();
        }

        public bool RemovePart(IBodyPart part, [NotNullWhen(true)] out string? slotName)
        {
            throw new NotImplementedException();
        }

        public List<IBodyPart> DropPart(IBodyPart part)
        {
            throw new NotImplementedException();
        }

        public bool ConnectedToCenter(IBodyPart part)
        {
            throw new NotImplementedException();
        }

        public bool HasSlot(string slot)
        {
            throw new NotImplementedException();
        }

        public bool TryGetPart(string slot, [NotNullWhen(true)] out IBodyPart? result)
        {
            throw new NotImplementedException();
        }

        public bool TryGetSlot(IBodyPart part, [NotNullWhen(true)] out string? slot)
        {
            throw new NotImplementedException();
        }

        public bool TryGetSlotType(string slot, out BodyPartType result)
        {
            throw new NotImplementedException();
        }

        public bool TryGetSlotConnections(string slot, [NotNullWhen(true)] out List<string>? connections)
        {
            throw new NotImplementedException();
        }

        public bool TryGetPartConnections(string slot, [NotNullWhen(true)] out List<IBodyPart>? connections)
        {
            throw new NotImplementedException();
        }

        public bool TryGetPartConnections(IBodyPart part, [NotNullWhen(true)] out List<IBodyPart>? connections)
        {
            throw new NotImplementedException();
        }

        public List<IBodyPart> GetPartsOfType(BodyPartType type)
        {
            throw new NotImplementedException();
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            // TODO Connections
            serializer.DataField(this, b => b.TemplateName, "template", string.Empty);

            serializer.DataField(this, b => b.Slots, "slots", new Dictionary<string, BodyPartType>());

            serializer.DataField(this, b => b.PartIDs, "partIds", new Dictionary<string, string>());
        }
    }

    [Serializable, NetSerializable]
    public sealed class BodyPartAddedMessage : ComponentMessage
    {
        public readonly string RSIPath;
        public readonly string RSIState;
        public readonly Enum RSIMap;

        public BodyPartAddedMessage(string rsiPath, string rsiState, Enum rsiMap)
        {
            Directed = true;
            RSIPath = rsiPath;
            RSIState = rsiState;
            RSIMap = rsiMap;
        }
    }

    [Serializable, NetSerializable]
    public sealed class BodyPartRemovedMessage : ComponentMessage
    {
        public readonly Enum RSIMap;
        public readonly EntityUid? Dropped;

        public BodyPartRemovedMessage(Enum rsiMap, EntityUid? dropped = null)
        {
            Directed = true;
            RSIMap = rsiMap;
            Dropped = dropped;
        }
    }

    [Serializable, NetSerializable]
    public sealed class MechanismSpriteAddedMessage : ComponentMessage
    {
        public readonly Enum RSIMap;

        public MechanismSpriteAddedMessage(Enum rsiMap)
        {
            Directed = true;
            RSIMap = rsiMap;
        }
    }

    [Serializable, NetSerializable]
    public sealed class MechanismSpriteRemovedMessage : ComponentMessage
    {
        public readonly Enum RSIMap;

        public MechanismSpriteRemovedMessage(Enum rsiMap)
        {
            Directed = true;
            RSIMap = rsiMap;
        }
    }

    /// <summary>
    ///     Defines a surgery operation that can be performed.
    /// </summary>
    [Serializable, NetSerializable]
    public enum SurgeryType
    {
        None = 0,
        Incision,
        Retraction,
        Cauterization,
        VesselCompression,
        Drilling,
        Amputation
    }
}
