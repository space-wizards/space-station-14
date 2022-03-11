using System;
using Robust.Shared.Serialization;

namespace Content.Shared.Markings
{
    [Serializable, NetSerializable]
    public enum MarkingCategories
    {
        Head,
        Chest,
        Arms,
        Legs,
        Ears,
        Tail,
        Overlay
    }
}
