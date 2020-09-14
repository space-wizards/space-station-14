#nullable enable
using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Body
{
    public abstract class SharedBodyComponent : DamageableComponent, IBody
    {
        public override string Name => "BodyManager";

        public override uint? NetID => ContentNetIDs.BODY;

        [ViewVariables]
        public string TemplateName { get; private set; }

        [ViewVariables]
        public Dictionary<string, BodyPartType> Slots { get; private set; } = new Dictionary<string, BodyPartType>();

        [ViewVariables]
        public Dictionary<string, List<string>> Connections { get; private set; } = new Dictionary<string, List<string>>();

        public BodyPreset Preset { get; }

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

        public bool RemovePart(IBodyPart part, out string? slotName)
        {
            throw new NotImplementedException();
        }

        public IEntity? DropPart(IBodyPart part)
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

        public bool TryGetPart(string slot, out IBodyPart? result)
        {
            throw new NotImplementedException();
        }

        public bool TryGetSlot(IBodyPart part, out string? slot)
        {
            throw new NotImplementedException();
        }

        public bool TryGetSlotType(string slot, out BodyPartType result)
        {
            throw new NotImplementedException();
        }

        public bool TryGetSlotConnections(string slot, out List<string>? connections)
        {
            throw new NotImplementedException();
        }

        public bool TryGetPartConnections(string slot, out List<IBodyPart>? connections)
        {
            throw new NotImplementedException();
        }

        public bool TryGetPartConnections(IBodyPart part, out List<IBodyPart>? connections)
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

            // TODO Slots/Connections
            serializer.DataField(this, b => b.TemplateName, "template", string.Empty);

            serializer.DataField(this, b => b.Slots, "slots", new Dictionary<string, BodyPartType>());
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
    ///     Used to determine whether a BodyPart can connect to another BodyPart.
    /// </summary>
    [Serializable, NetSerializable]
    public enum BodyPartCompatibility
    {
        Universal = 0,
        Biological,
        Mechanical
    }

    /// <summary>
    ///     Each BodyPart has a BodyPartType used to determine a variety of things.
    ///     For instance, what slots it can fit into.
    /// </summary>
    [Serializable, NetSerializable]
    public enum BodyPartType
    {
        Other = 0,
        Torso,
        Head,
        Arm,
        Hand,
        Leg,
        Foot
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
