using System.Linq;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.FeedbackSystem;

public sealed partial class SharedFeedbackSystem : EntitySystem
{
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    public List<ProtoId<FeedbackPopupPrototype>> FeedbackPopupProtoIds = new();

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
            .Select(x => (ProtoId<FeedbackPopupPrototype>) x.ID)
            .ToList();
        FeedbackPopupProtoIds.Sort();
    }

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
