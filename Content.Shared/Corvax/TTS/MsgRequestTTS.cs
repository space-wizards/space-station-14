using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Corvax.TTS;

// ReSharper disable once InconsistentNaming
public sealed class MsgRequestTTS : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public EntityUid Uid { get; set; } = EntityUid.Invalid;
    public string Text { get; set; } = String.Empty;
    public string VoiceId { get; set; } = String.Empty;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Uid = new EntityUid(buffer.ReadInt32());
        Text = buffer.ReadString();
        VoiceId = buffer.ReadString();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write((int)Uid);
        buffer.Write(Text);
        buffer.Write(VoiceId);
    }
}
