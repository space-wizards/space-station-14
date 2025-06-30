using Robust.Shared.Serialization;

namespace Content.Shared._Starlight.CloudEmotes
{
    [Serializable, NetSerializable]
    public sealed class CloudEmotesMessage : EntityEventArgs
    {
        public NetEntity Uid;
        public string Emote;

        public CloudEmotesMessage(NetEntity uid, string emote)
        {
            Uid = uid;
            Emote = emote;
        }
    }
}