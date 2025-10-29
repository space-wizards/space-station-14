using System.Linq;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.FeedbackSystem;

/// <summary>
/// SharedFeedbackManager handles feedback popup management and distribution across sessions.
/// It manages the state of displayed popups and provides mechanisms for opening, displaying, removing,
/// and sending popups to specified sessions or all sessions.
/// </summary>
public interface ISharedFeedbackManager
{
    /// <summary>
    /// An event that is triggered whenever the set of displayed feedback popups changes.<br/>
    /// The boolean parameter is true if new popups have been added
    /// </summary>
    event Action<bool>? DisplayedPopupsChanged;

    /// <summary>
    /// Adds the specified popup prototypes to the displayed popups on the client..
    /// </summary>
    /// <param name="prototypes">A list of popup prototype IDs to be added to the displayed prototypes</param>
    /// <remarks>
    /// This does nothing on the server.
    /// <br/>
    /// Use this if you want to add a popup from a shared or client-side entity system.
    /// </remarks>
    void Display(List<ProtoId<FeedbackPopupPrototype>>? prototypes) {}

    /// <summary>
    /// Removes the specified popup prototypes from the displayed popups on the client.
    /// </summary>
    /// <param name="prototypes">A list of popup prototype IDs to be removed from the displayed prototypes.
    /// If null, all displayed popups will be cleared.</param>
    /// <remarks>This does nothing on the server.</remarks>
    void Remove(List<ProtoId<FeedbackPopupPrototype>>? prototypes) {}

    /// <summary>
    /// Sends a list of feedback popup prototypes to a specific entity, identified by its EntityUid.
    /// </summary>
    /// <param name="uid">The unique identifier of the entity to send the feedback popups to.</param>
    /// <param name="popupPrototypes">The list of feedback popup prototypes to send to the entity.</param>
    /// <returns>Returns true if the feedback popups were successfully sent, otherwise false.</returns>
    /// <remarks>This does nothing on the client.</remarks>
    bool Send(EntityUid uid, List<ProtoId<FeedbackPopupPrototype>> popupPrototypes)
    {
        return false;
    }

    /// <summary>
    /// Sends a list of feedback popup prototypes to the specified session.
    /// </summary>
    /// <param name="session">The session to which the feedback popups will be sent.</param>
    /// <param name="popupPrototypes">A list of feedback popup prototype IDs to send to the session.</param>
    /// <param name="remove">When true, removes the specified prototypes instead of adding them</param>
    /// <remarks>This does nothing on the client.</remarks>
    void SendToSession(ICommonSession session, List<ProtoId<FeedbackPopupPrototype>> popupPrototypes, bool remove = false) {}

    /// <summary>
    /// Sends the specified feedback popup prototypes to all connected client sessions.
    /// </summary>
    /// <param name="popupPrototypes">A list of popup prototype IDs to be sent to all connected sessions.</param>
    /// <param name="remove">When true, removes the specified prototypes instead of adding them</param>
    /// <remarks>This does nothing on the client.</remarks>
    void SendToAllSessions(List<ProtoId<FeedbackPopupPrototype>> popupPrototypes, bool remove = false) {}

    /// <summary>
    /// Opens the feedback popup for a specific session.
    /// </summary>
    /// <param name="session">The session for which the feedback popup should be opened.</param>
    /// <remarks>This does nothing on the client.</remarks>
    void OpenForSession(ICommonSession session) {}

    /// <summary>
    /// Opens the feedback popup for all connected sessions.
    /// </summary>
    /// <remarks>This does nothing on the client.</remarks>
    void OpenForAllSessions() {}
}

/// <inheritdoc cref="ISharedFeedbackManager" />
public abstract partial class SharedFeedbackManager : ISharedFeedbackManager
{
    [Dependency] private readonly IPrototypeManager _proto = null!;
    [Dependency] protected readonly INetManager NetManager = null!;

    public virtual IReadOnlySet<ProtoId<FeedbackPopupPrototype>>? DisplayedPopups => null;

    // <inheritdoc />
    public event Action<bool>? DisplayedPopupsChanged;

    /// <summary>
    /// List of valid origns of the feedback popup that is filled from the CCVar. See
    /// <see cref="Content.Shared.CCVar.CCVars.FeedbackValidOrigins">FeedbackValidOrigins</see>
    /// </summary>
    private List<string> _validOrigins = [];

    [MustCallBase]
    public virtual void Initialize()
    {
        InitSubscriptions();
    }

    /// <inheritdoc />
    public virtual void Display(List<ProtoId<FeedbackPopupPrototype>>? prototypes) {}

    /// <inheritdoc />
    public virtual void Remove(List<ProtoId<FeedbackPopupPrototype>>? prototypes) {}

    /// <inheritdoc />
    public virtual bool Send(EntityUid uid, List<ProtoId<FeedbackPopupPrototype>> popupPrototypes)
    {
        return false;
    }

    /// <inheritdoc />
    public virtual void SendToSession(ICommonSession session, List<ProtoId<FeedbackPopupPrototype>> popupPrototypes, bool remove = false) {}

    /// <inheritdoc />
    public virtual void SendToAllSessions(List<ProtoId<FeedbackPopupPrototype>> popupPrototypes, bool remove = false) {}

    /// <inheritdoc />
    public virtual void OpenForSession(ICommonSession session) {}

    /// <inheritdoc />
    public virtual void OpenForAllSessions() {}

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

    protected void InvokeDisplayedPopupsChanged(bool show)
    {
        DisplayedPopupsChanged?.Invoke(show);
    }
}
