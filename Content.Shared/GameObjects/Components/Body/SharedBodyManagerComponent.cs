using System;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Body
{
    public abstract class SharedBodyManagerComponent : DamageableComponent, ISharedBodyManagerComponent
    {
        public override string Name => "BodyManager";

        public override uint? NetID => ContentNetIDs.BODY_MANAGER;
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
