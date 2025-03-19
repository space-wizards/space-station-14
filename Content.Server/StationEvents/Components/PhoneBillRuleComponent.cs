using Robust.Shared.Audio;

namespace Content.Server.StationEvents.Components;

[RegisterComponent]
public sealed partial class PhoneBillRuleComponent : Component
{
    /// <summary>
    ///     How long until payment is collected.
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.Zero;

    /// <summary>
    ///     Cost of the phone bill per PDA or ID.
    /// </summary>
    [DataField]
    public int Price = 0;

    /// <summary>
    ///     The sound that plays when the first announcement is sent.
    /// </summary>
    [DataField]
    public SoundSpecifier? InitialSound = new SoundPathSpecifier("/Audio/Misc/notice1.ogg");

    /// <summary>
    ///     The sound that plays when a failure announcement is sent.
    /// </summary>
    [DataField]
    public SoundSpecifier? FailureSound = new SoundPathSpecifier("/Audio/Effects/sadtrombone.ogg");

    /// <summary>
    ///     The sound that plays when a success announcement is sent.
    /// </summary>
    [DataField]
    public SoundSpecifier? SuccessSound = new SoundPathSpecifier("/Audio/Announcements/announce.ogg");
}
