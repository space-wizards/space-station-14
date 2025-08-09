using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.FeedbackSystem;

public sealed partial class SharedFeedbackSystem : EntitySystem
{
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    public bool SendPopups(EntityUid? uid, List<ProtoId<FeedbackPopupPrototype>> popupPrototypes)
    {
        if (uid == null)
            return false;

        if (!_player.TryGetSessionByEntity(uid.Value, out var session))
            return false;

        return SendPopupsSession(session, popupPrototypes);
    }

    public bool SendPopupsSession(ICommonSession session, List<ProtoId<FeedbackPopupPrototype>> popupPrototypes)
    {
        var msg = new FeedbackPopupMessage(popupPrototypes);
        RaiseNetworkEvent(msg, session);

        return true;
    }
}
