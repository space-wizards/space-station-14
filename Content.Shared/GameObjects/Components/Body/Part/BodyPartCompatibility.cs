using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Body.Part
{
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
}
