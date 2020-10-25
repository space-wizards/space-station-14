using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Body.Part
{
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
}
