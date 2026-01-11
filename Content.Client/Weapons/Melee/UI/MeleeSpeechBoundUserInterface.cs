using Content.Shared.Speech.Components;
using Robust.Client.UserInterface;

namespace Content.Client.Weapons.Melee.UI;

/// <summary>
/// Initializes a <see cref="MeleeSpeechWindow"/> and updates it according to the component's current data.
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

        Update();
    }

    private void OnBattlecryChanged(string newBattlecry)
    {
        SendPredictedMessage(new MeleeSpeechBattlecryChangedMessage(newBattlecry));
    }

    public override void Update()
    {
        base.Update();

        if (EntMan.TryGetComponent<MeleeSpeechComponent>(Owner, out var meleeSpeechComp))
            _window?.SetCurrentBattlecry(meleeSpeechComp.Battlecry);
    }
}
