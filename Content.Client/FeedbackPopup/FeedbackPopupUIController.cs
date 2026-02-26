using Content.Shared.FeedbackSystem;
using Content.Shared.GameTicking;
using Robust.Client.UserInterface.Controllers;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.FeedbackPopup;

/// <summary>
/// This handles getting feedback popup messages from the server and making a popup in the client.
/// </summary>
[UsedImplicitly]
public sealed class FeedbackPopupUIController : UIController
{
    [Dependency] private readonly ClientFeedbackManager _feedbackManager = null!;
    [Dependency] private readonly IPrototypeManager _proto = null!;
    [Dependency] private readonly IUriOpener _uri = null!;

    private FeedbackPopupWindow _window = null!;

    public override void Initialize()
    {
        _window = new FeedbackPopupWindow(_proto, _uri);

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        SubscribeNetworkEvent<RoundEndMessageEvent>(OnRoundEnd);

        _feedbackManager.DisplayedPopupsChanged += OnPopupsChanged;
    }

    public void ToggleWindow()
    {
        if (_window.IsOpen)
        {
            _window.Close();
        }
        else
        {
            _window.OpenCentered();
        }
    }

    private void OnRoundEnd(RoundEndMessageEvent ev, EntitySessionEventArgs args)
    {
        // Add round end prototypes.
        var roundEndPrototypes = _feedbackManager.GetOriginFeedbackPrototypes(true);
        if (roundEndPrototypes.Count == 0)
            return;

        _feedbackManager.Display(roundEndPrototypes);

        // Even if no new prototypes were added, we still want to open the window.
        if (!_window.IsOpen)
            _window.OpenCentered();
    }

    private void OnPopupsChanged(bool newPopups)
    {
        UpdateWindow(_feedbackManager.DisplayedPopups);

        if (newPopups && !_window.IsOpen)
            _window.OpenCentered();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        UpdateWindow(_feedbackManager.DisplayedPopups);
    }

    private void UpdateWindow(IReadOnlyCollection<ProtoId<FeedbackPopupPrototype>> prototypes)
    {
        _window.Update(prototypes);
    }
}
