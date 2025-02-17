/// by ModerN, mailto:modern-nm@yandex.by or https://github.com/modern-nm. Discord: modern.df
namespace Content.Shared.ADT
{
    /// <summary>
    /// Base emote emitter.
    /// </summary>
    public abstract partial class BaseEmitEmoteComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("emote", required: true)]
        public string? EmoteType { get; set; }
    }
}
