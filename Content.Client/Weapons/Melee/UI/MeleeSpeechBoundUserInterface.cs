using Content.Shared.Speech.Components;
using Robust.Client.UserInterface;

namespace Content.Client.Weapons.Melee.UI;

/// <summary>
/// Initializes a <see cref="MeleeSpeechWindow"/> and updates it when new server messages are received.
/// </summary>
public sealed class MeleeSpeechBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private MeleeSpeechWindow? _window;

    public MeleeSpeechBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<MeleeSpeechWindow>();
        _window.OnBattlecryEntered += OnBattlecryChanged;
    }

    private void OnBattlecryChanged(string newBattlecry)
    {
        SendMessage(new MeleeSpeechBattlecryChangedMessage(newBattlecry));
    }

    /// <summary>
    /// Update the UI state based on server-sent info
    /// </summary>
    /// <param name="state"></param>
    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (_window == null || state is not MeleeSpeechBoundUserInterfaceState cast)
            return;

        _window.SetCurrentBattlecry(cast.CurrentBattlecry);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        _window?.Dispose();
    }
}
