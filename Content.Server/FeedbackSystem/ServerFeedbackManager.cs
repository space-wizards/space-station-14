using Content.Shared.FeedbackSystem;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.FeedbackSystem;

/// <inheritdoc />
public sealed class ServerFeedbackManager : SharedFeedbackManager
{
    [Dependency] private readonly ISharedPlayerManager _player = null!;

    public override void Initialize()
    {
        base.Initialize();
        NetManager.RegisterNetMessage<FeedbackPopupMessage>();
        NetManager.RegisterNetMessage<OpenFeedbackPopupMessage>();
    }

    /// <inheritdoc />
    public override bool Send(EntityUid uid, List<ProtoId<FeedbackPopupPrototype>> popupPrototypes)
    {
        if (!_player.TryGetSessionByEntity(uid, out var session))
            return false;

        SendToSession(session, popupPrototypes);
        return true;
    }

    /// <inheritdoc />
    public override void SendToSession(ICommonSession session, List<ProtoId<FeedbackPopupPrototype>> popupPrototypes, bool remove = false)
    {
        if (!NetManager.IsServer)
            return;

        var msg = new FeedbackPopupMessage
        {
            FeedbackPrototypes = popupPrototypes,
            Remove = remove,
        };

        NetManager.ServerSendMessage(msg, session.Channel);
    }

    /// <inheritdoc />
    public override void SendToAllSessions(List<ProtoId<FeedbackPopupPrototype>> popupPrototypes, bool remove = false)
    {
        if (!NetManager.IsServer)
            return;

        var msg = new FeedbackPopupMessage
        {
            FeedbackPrototypes = popupPrototypes,
            Remove = remove,
        };

        NetManager.ServerSendToAll(msg);
    }

    /// <inheritdoc />
    public override void OpenForSession(ICommonSession session)
    {
        if (!NetManager.IsServer)
            return;

        var msg = new OpenFeedbackPopupMessage();
        NetManager.ServerSendMessage(msg, session.Channel);
    }

    /// <inheritdoc />
    public override void OpenForAllSessions()
    {
        if (!NetManager.IsServer)
            return;

        var msg = new OpenFeedbackPopupMessage();
        NetManager.ServerSendToAll(msg);
    }
}
