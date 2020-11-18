using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Body.Part
{
    /// <summary>
    ///     Determines whether two <see cref="IBodyPart"/>s can connect.
    /// </summary>
    [Serializable, NetSerializable]
    public enum BodyPartCompatibility
    {
        Universal = 0,
        Biological,
        Mechanical
    }
}
