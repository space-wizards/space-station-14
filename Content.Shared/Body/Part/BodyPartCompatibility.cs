using System;
using Content.Shared.Body.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Body.Part
{
    //TODO: This should be a prototype. --DrSmugleaf
    /// <summary>
    ///     Determines whether two <see cref="SharedBodyPartComponent"/>s can connect.
    /// </summary>
    [Serializable, NetSerializable]
    public enum BodyPartCompatibility
    {
        Universal = 0,
        Biological,
        Mechanical,
        Slime
    }
}
