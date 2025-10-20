using Content.Shared.Administration.Managers.Bwoink;
using JetBrains.Annotations;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Managers.Bwoink;

public sealed partial class ServerBwoinkManager
{
    /// <summary>
    /// Sends a message to a given channel and user channel using the system user.
    /// </summary>
    [PublicAPI]
    public void SendMessageInChannel(ProtoId<BwoinkChannelPrototype> channel, NetUserId userChannel, string text, MessageFlags flags)
    {
        SynchronizeMessage(channel,
            userChannel,
            new BwoinkMessage(_localizationManager.GetString("bwoink-system-user"),
                null,
                DateTime.UtcNow,
                text,
                flags));
    }
}
