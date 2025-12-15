using Content.Shared.Administration.Managers.Bwoink;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.Administration.Managers;

public sealed partial class ClientBwoinkManager
{
    /// <summary>
    /// Local cache that holds the values we use for "predicting" our access for channels.
    /// Since access is pure server side (it requires stuff the client doesn't have) we need it.
    /// </summary>
    /// <remarks>
    /// Controlled by <see cref="MsgBwoinkSyncChannels"/> and <see cref="MsgBwoinkSyncChannelsRequest"/>
    /// </remarks>
    [ViewVariables]
    private Dictionary<ProtoId<BwoinkChannelPrototype>, BwoinkChannelConditionFlags> _accessCache = new();

    private void SyncChannels(MsgBwoinkSyncChannels message)
    {
        _accessCache = message.Channels;
        InvokeReloadedData();
    }

    public void RequestChannels()
    {
        _netManager.ClientSendMessage(new MsgBwoinkSyncChannelsRequest());
    }

    public bool CanManageChannel(ProtoId<BwoinkChannelPrototype> proto)
    {
        return CanManageChannel(proto, PlayerManager.LocalSession!);
    }

    public override bool CanManageChannel(ProtoId<BwoinkChannelPrototype> proto, ICommonSession session)
    {
        if (PlayerManager.LocalSession != session)
            throw new InvalidOperationException("Can only get condition status for local player!");

        return _accessCache[proto].HasFlag(BwoinkChannelConditionFlags.Manager);
    }

    public override bool CanManageChannel(BwoinkChannelPrototype channel, ICommonSession session)
    {
        return CanManageChannel(channel.ID, session);
    }

    public bool CanReadChannel(ProtoId<BwoinkChannelPrototype> proto)
    {
        return CanReadChannel(proto, PlayerManager.LocalSession!);
    }

    public override bool CanReadChannel(ProtoId<BwoinkChannelPrototype> proto, ICommonSession session)
    {
        if (PlayerManager.LocalSession != session)
            throw new InvalidOperationException("Can only get condition status for local player!");

        return _accessCache[proto].HasFlag(BwoinkChannelConditionFlags.Read);
    }

    public override bool CanReadChannel(BwoinkChannelPrototype channel, ICommonSession session)
    {
        return CanReadChannel(channel.ID, session);
    }

    public bool CanWriteChannel(ProtoId<BwoinkChannelPrototype> proto)
    {
        return CanWriteChannel(proto, PlayerManager.LocalSession!);
    }

    public override bool CanWriteChannel(ProtoId<BwoinkChannelPrototype> proto, ICommonSession session)
    {
        if (PlayerManager.LocalSession != session)
            throw new InvalidOperationException("Can only get condition status for local player!");

        return _accessCache[proto].HasFlag(BwoinkChannelConditionFlags.Write);
    }

    public override bool CanWriteChannel(BwoinkChannelPrototype channel, ICommonSession session)
    {
        return CanWriteChannel(channel.ID, session);
    }
}
