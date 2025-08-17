using System.Linq;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.FeedbackSystem;

public sealed partial class SharedFeedbackSystem : EntitySystem
{
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    public List<string> FeedbackPopupProtoIds = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        LoadPrototypes();
        EventInitialize();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<FeedbackPopupPrototype>())
            return;

        LoadPrototypes();
    }

    /// <summary>
    ///     Load all the prototype IDs into FeedbackPopupProtoIds.
    /// </summary>
    private void LoadPrototypes()
    {
        FeedbackPopupProtoIds = _proto.EnumeratePrototypes<FeedbackPopupPrototype>()
            .Select(x => x.ID)
            .Order()
            .ToList();
    }

    public bool SendPopups(EntityUid uid, List<ProtoId<FeedbackPopupPrototype>> popupPrototypes)
    {
        if (!_player.TryGetSessionByEntity(uid, out var session))
            return false;

        SendPopupsSession(session, popupPrototypes);
        return true;
    }

    public void SendPopupsSession(ICommonSession session, List<ProtoId<FeedbackPopupPrototype>> popupPrototypes)
    {
        var msg = new FeedbackPopupMessage(popupPrototypes);
        RaiseNetworkEvent(msg, session);

        return;
    }
}
