using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Piping.Unary.Visuals
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
