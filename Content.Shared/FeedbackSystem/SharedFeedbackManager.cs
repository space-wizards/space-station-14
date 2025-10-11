using System.Linq;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.FeedbackSystem;

public sealed partial class SharedFeedbackManager : IDisposable
{
    [Dependency] private readonly ISharedPlayerManager _player = null!;
    [Dependency] private readonly IPrototypeManager _proto = null!;
    [Dependency] private readonly INetManager _netManager = null!;

    /// <summary>
    /// An event that is triggered whenever the set of displayed feedback popups changes.<br/>
    /// The boolean parameter is true if new popups have been added
    /// </summary>
    public event Action<bool>? DisplayedPopupsChanged;

    /// <summary>
    /// A read-only set representing the currently displayed feedback popups.
    /// </summary>
    /// <remarks>This shouldn't be used on the server</remarks>
    public IReadOnlySet<ProtoId<FeedbackPopupPrototype>> DisplayedPopups => _displayedPopups;

    private readonly HashSet<ProtoId<FeedbackPopupPrototype>> _displayedPopups = [];

    private List<string> _validOrigins = [];

    public void Initialize()
    {
        _netManager.RegisterNetMessage<FeedbackPopupMessage>(ReceivedPopupMessage, NetMessageAccept.Client);
        InitSubscriptions();
    }

    public void Dispose()
    {
        DisposeSubscriptions();
    }

    /// <summary>
    /// Adds the specified popup prototypes to the displayed popups on the client..
    /// </summary>
    /// <param name="prototypes">A list of popup prototype IDs to be added to the displayed prototypes</param>
    /// <remarks>This does nothing on the server</remarks>
    public void Display(List<ProtoId<FeedbackPopupPrototype>>? prototypes)
    {
        if (prototypes == null || !_netManager.IsClient)
            return;

        var count = _displayedPopups.Count;
        _displayedPopups.UnionWith(prototypes);
        DisplayedPopupsChanged?.Invoke(_displayedPopups.Count > count);
    }

    /// <summary>
    /// Removes the specified popup prototypes from the displayed popups on the client.
    /// </summary>
    /// <param name="prototypes">A list of popup prototype IDs to be removed from the displayed prototypes.
    /// If null, all displayed popups will be cleared.</param>
    /// <remarks>This does nothing on the server.</remarks>
    public void Remove(List<ProtoId<FeedbackPopupPrototype>>? prototypes)
    {
        if (!_netManager.IsClient)
            return;

        if (prototypes == null)
        {
            _displayedPopups.Clear();
        }
        else
        {
            _displayedPopups.ExceptWith(prototypes);
        }

        DisplayedPopupsChanged?.Invoke(false);
    }

    /// <summary>
    /// Sends a list of feedback popup prototypes to a specific entity, identified by its EntityUid.
    /// </summary>
    /// <param name="uid">The unique identifier of the entity to send the feedback popups to.</param>
    /// <param name="popupPrototypes">The list of feedback popup prototypes to send to the entity.</param>
    /// <returns>Returns true if the feedback popups were successfully sent, otherwise false.</returns>
    public bool Send(EntityUid uid, List<ProtoId<FeedbackPopupPrototype>> popupPrototypes)
    {
        if (!_player.TryGetSessionByEntity(uid, out var session))
            return false;

        SendToSession(session, popupPrototypes);
        return true;
    }

    /// <summary>
    /// Sends a list of feedback popup prototypes to the specified session.
    /// </summary>
    /// <param name="session">The session to which the feedback popups will be sent.</param>
    /// <param name="popupPrototypes">A list of feedback popup prototype IDs to send to the session.</param>
    public void SendToSession(ICommonSession session, List<ProtoId<FeedbackPopupPrototype>> popupPrototypes)
    {
        if (!_netManager.IsServer)
            return;

        var msg = new FeedbackPopupMessage
        {
            FeedbackPrototypes = popupPrototypes,
        };

        _netManager.ServerSendMessage(msg, session.Channel);
    }

    /// <summary>
    /// Sends the specified feedback popup prototypes to all connected client sessions.
    /// </summary>
    /// <param name="popupPrototypes">A list of popup prototype IDs to be sent to all connected sessions.</param>
    public void SendToAllSessions(List<ProtoId<FeedbackPopupPrototype>> popupPrototypes)
    {
        if (!_netManager.IsServer)
            return;

        var msg = new FeedbackPopupMessage
        {
            FeedbackPrototypes = popupPrototypes,
        };

        _netManager.ServerSendToAll(msg);
    }

    /// <summary>
    /// Get a list of feedback prototypes that match the current valid origins.
    /// </summary>
    /// <param name="roundEndOnly">If true, only retrieve pop-ups with ShowRoundEnd set to true.</param>
    /// <returns>Returns a list of protoIds; possibly empty.</returns>
    public List<ProtoId<FeedbackPopupPrototype>> GetOriginFeedbackPrototypes(bool roundEndOnly)
    {
        var feedbackProtypes = _proto.EnumeratePrototypes<FeedbackPopupPrototype>()
            .Where(x => (!roundEndOnly || x.ShowRoundEnd) && _validOrigins.Contains(x.PopupOrigin))
            .Select(x => new ProtoId<FeedbackPopupPrototype>(x.ID))
            .OrderBy(x => x.Id)
            .ToList();

        return feedbackProtypes;
    }

    private void ReceivedPopupMessage(FeedbackPopupMessage message)
    {
        if (message.Remove)
        {
            Remove(message.FeedbackPrototypes);
            return;
        }

        Display(message.FeedbackPrototypes);
    }
}
