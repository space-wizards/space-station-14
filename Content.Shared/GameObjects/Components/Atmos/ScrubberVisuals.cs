using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Atmos
{
    [Serializable, NetSerializable]
    public enum ScrubberVisuals : byte
    {
        State,
    }

    [Serializable, NetSerializable]
    public enum ScrubberState : byte
    {
        Off,
        Scrub,
        Siphon,
        WideScrub,
        Welded,
    }
}
