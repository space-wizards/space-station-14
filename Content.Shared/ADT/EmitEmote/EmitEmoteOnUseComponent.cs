/// by ModerN, mailto:modern-nm@yandex.by or https://github.com/modern-nm. Discord: modern.df
namespace Content.Shared.ADT
{
    /// <summary>
    /// This component can be used for send emote-message to clients when some custom item was used.
    /// </summary>
    [RegisterComponent]
    public sealed partial class EmitEmoteOnUseComponent : BaseEmitEmoteComponent
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("handle")]
        public bool Handle = true;
    }
}
