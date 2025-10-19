// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

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
