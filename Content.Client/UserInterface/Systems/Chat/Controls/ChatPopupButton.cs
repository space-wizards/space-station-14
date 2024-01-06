using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface.Systems.Chat.Controls;

// NO MORE FUCKING COPY PASTING THIS SHIT

/// <summary>
/// Base class for button that toggles a popup-window.
/// Base type of <see cref="ChannelFilterButton"/> and <see cref="ChannelSelectorButton"/>.
/// </summary>
public abstract class ChatPopupButton<TPopup> : Button
    where TPopup : Popup, new()
{
    private readonly IGameTiming _gameTiming;

    public readonly TPopup Popup;

    private uint _frameLastPopupChanged;

    protected ChatPopupButton()
    {
        _gameTiming = IoCManager.Resolve<IGameTiming>();

        ToggleMode = true;
        OnToggled += OnButtonToggled;

        Popup = UserInterfaceManager.CreatePopup<TPopup>();
        Popup.OnVisibilityChanged += OnPopupVisibilityChanged;
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        // If you try to close the popup by clicking on the button again the following would happen:
        // The UI system would see that you clicked outside a popup, and would close it.
        // Because of the above logic, that sets the button to UNPRESSED.
        // THEN, it would propagate the keyboard event to us, the chat selector...
        // And we would become pressed again.
        // As a workaround, we check the frame the popup was last dismissed (above)
        // and don't allow changing it again this frame.
        if (_frameLastPopupChanged == _gameTiming.CurFrame)
            return;

        base.KeyBindDown(args);
    }

    protected abstract UIBox2 GetPopupPosition();

    private void OnButtonToggled(ButtonToggledEventArgs args)
    {
        if (args.Pressed)
        {
            Popup.Open(GetPopupPosition());
        }
        else
        {
            Popup.Close();
        }
    }

    private void OnPopupVisibilityChanged(Control control)
    {
        // If the popup gets closed (e.g. by clicking anywhere else on the screen)
        // We clear the button pressed state.

        Pressed = control.Visible;
        _frameLastPopupChanged = _gameTiming.CurFrame;
    }
}
