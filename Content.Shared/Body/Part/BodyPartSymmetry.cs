#nullable enable
using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Body.Part
{
    /// <summary>
    ///     Defines the symmetry of a <see cref="IBodyPart"/>.
    /// </summary>
    [Serializable, NetSerializable]
    public enum BodyPartSymmetry
    {
        None = 0,
        Left,
        Right
    }
}
