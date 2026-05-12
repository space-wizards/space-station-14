using Content.Shared.FeedbackSystem;
using Robust.Shared.Prototypes;

namespace Content.Client.FeedbackPopup;

/// <inheritdoc />
public sealed class ClientFeedbackManager : SharedFeedbackManager
{
    /// <summary>
    /// A read-only set representing the currently displayed feedback popups.
    /// </summary>
    public override IReadOnlySet<ProtoId<FeedbackPopupPrototype>> DisplayedPopups => _displayedPopups;

    private readonly HashSet<ProtoId<FeedbackPopupPrototype>> _displayedPopups = [];

    public override void Initialize()
    {
        base.Initialize();
        NetManager.RegisterNetMessage<FeedbackPopupMessage>(ReceivedPopupMessage);
        NetManager.RegisterNetMessage<OpenFeedbackPopupMessage>(_ => Open());
    }

    /// <summary>
    /// Opens the feedback popup window.
    /// </summary>
    public void Open()
    {
        InvokeDisplayedPopupsChanged(true);
    }

    /// <inheritdoc />
    public override void Display(List<ProtoId<FeedbackPopupPrototype>>? prototypes)
    {
        if (prototypes == null || !NetManager.IsClient)
            return;

        var count = _displayedPopups.Count;
        _displayedPopups.UnionWith(prototypes);
        InvokeDisplayedPopupsChanged(_displayedPopups.Count > count);
    }

    /// <inheritdoc />
    public override void Remove(List<ProtoId<FeedbackPopupPrototype>>? prototypes)
    {
        if (!NetManager.IsClient)
            return;

        if (prototypes == null)
        {
            _displayedPopups.Clear();
        }
        else
        {
            _displayedPopups.ExceptWith(prototypes);
        }

        InvokeDisplayedPopupsChanged(false);
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
