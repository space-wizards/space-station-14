﻿using Content.Shared.Dataset;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Humanoid
{
    // You need to update profile, profile editor, maybe voices and names if you want to expand this further.
    public enum Sex : byte
    {
        Male,
        Female,
        Unsexed,
    }

    /// <summary>
    ///     Raised when entity has changed their voice.
    ///     This doesn't handle gender changes.
    /// </summary>
    public record struct VoiceChangedEvent(Sex OldVoice, Sex NewVoice);
}
