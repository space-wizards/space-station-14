using Content.Shared.FeedbackSystem;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client.FeedbackPopup;

/// <summary>
/// This handles getting feedback popup messages from the server and making a popup in the client.
/// </summary>
public sealed class FeedbackPopupUIController : UIController
{
    private FeedbackPopupWindow? _window;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<FeedbackPopupMessage>(OnFeedbackPopup);
    }

    private void OnFeedbackPopup(FeedbackPopupMessage msg, EntitySessionEventArgs args)
    {
        // If a window is already open, close it
        _window?.Close();

        _window = new FeedbackPopupWindow();
        _window.Update(msg.FeedbackPrototypes);
        _window.OpenCentered();
        _window.OnClose += () => _window = null;
    }
}
