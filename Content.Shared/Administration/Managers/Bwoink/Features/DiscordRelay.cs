namespace Content.Shared.Administration.Managers.Bwoink.Features;

/// <summary>
/// This bwoink channel will have its contents relayed to Discord.
/// </summary>
public sealed partial class DiscordRelay : BwoinkChannelFeature
{
    /// <summary>
    /// A CVar Def for the channel to use.
    /// </summary>
    [DataField(required:true)]
    public string ChannelCvar { get; set; }
}
