using Content.Shared.Kitchen.Components;
using Content.Shared.Kitchen.EntitySystems;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Kitchen.UI;

[UsedImplicitly]
public sealed partial class MicrowaveBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private MicrowaveSystem _microwave = default!;

    [ViewVariables]
    private MicrowaveMenu? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<MicrowaveMenu>();

        _menu.OnStartCook += () =>
            SendPredictedMessage(new MicrowaveStartCookMessage());

        _menu.OnEjectAll += () =>
            SendPredictedMessage(new MicrowaveEjectMessage());

        _menu.OnEjectSolid += entity =>
            SendPredictedMessage(new MicrowaveEjectSolidIndexedMessage(EntMan.GetNetEntity(entity)));

        _menu.OnCookTimeSelected += (buttonIndex, cookTime) =>
            SendPredictedMessage(new MicrowaveSelectCookTimeMessage(buttonIndex, cookTime));

        Update();
    }

    public override void Update()
    {
        base.Update();

        if (_menu == null || !EntMan.TryGetComponent<MicrowaveComponent>(Owner, out var comp))
            return;

        var contents = _microwave.GetMicrowaveContents(Owner);
        var isBusy = EntMan.TryGetComponent<ActiveMicrowaveComponent>(Owner, out var active);
        _menu.UpdateUi(contents, isBusy, active?.CookTimeEnd ?? TimeSpan.Zero,
            comp.CurrentCookTimerTime, comp.CurrentCookTimeButtonIndex);
    }
}
