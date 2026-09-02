using Content.Shared.Speech.Components;
using Robust.Client.UserInterface;

namespace Content.Client.Weapons.Melee.UI;

/// <summary>
/// A BUI to set the battlecry for an entity.
/// </summary>
/// <remarks>
/// Initializes a <see cref="MeleeSpeechWindow"/> and updates it when new server messages are received.
/// </remarks>
/// <seealso cref="MeleeSpeechComponent"/>
public sealed partial class MeleeSpeechBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private MeleeSpeechWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<MeleeSpeechWindow>();

        if (EntMan.TryGetComponent(Owner, out MeleeSpeechComponent? speech))
        {
            _window.SetInitialBattlecry(speech!.Battlecry);
            _window.SetMaxBattlecryLength(speech!.MaxBattlecryLength);
        }

        _window.OnBattlecryChanged += OnBattlecryChanged;
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
}
