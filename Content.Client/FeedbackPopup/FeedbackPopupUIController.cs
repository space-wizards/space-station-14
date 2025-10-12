using Content.Shared.FeedbackSystem;
using Content.Shared.GameTicking;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Input.Binding;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Client.FeedbackPopup;

/// <summary>
/// This handles getting feedback popup messages from the server and making a popup in the client.
/// </summary>
[UsedImplicitly]
public sealed class FeedbackPopupUIController : UIController
{
    [Dependency] private readonly SharedFeedbackManager _feedbackManager = null!;

    private FeedbackPopupWindow _window = null!;

    public override void Initialize()
    {
        _window = UIManager.CreateWindow<FeedbackPopupWindow>();

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
