using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.GameTicking;

[Serializable, NetSerializable]
public sealed class AutoRoundEndingHudEvent : EntityEventArgs
{
    // Game time when the in-round timing started
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan StartTime;

    // How many seconds from StartTime until round end
    [DataField]
    public float DelaySeconds;

    // Optional label to display near the timer
    [DataField]
    public string? Label;

    // RSI path relative to /Textures ("Objects/.../interface.rsi")
    [DataField]
    public string? HudIconRsi;

    // RSI state within the RSI
    [DataField]
    public string? HudIconState;

    public AutoRoundEndingHudEvent() {}

    public AutoRoundEndingHudEvent(TimeSpan startTime, float delaySeconds, string? label, string? hudIconRsi, string? hudIconState)
    {
        StartTime = startTime;
        DelaySeconds = delaySeconds;
        Label = label;
        HudIconRsi = hudIconRsi;
        HudIconState = hudIconState;
    }
}

[Serializable, NetSerializable]
public sealed class AutoRoundEndingHudClearEvent : EntityEventArgs
{
}
