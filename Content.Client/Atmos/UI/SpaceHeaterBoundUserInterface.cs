using Content.Client.Power.Components;
using Content.Client.Power.EntitySystems;
using Content.Shared.Atmos.Piping.Portable.Components;
using Content.Shared.Atmos.Piping.Unary.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Atmos.UI;

/// <summary>
/// Initializes a <see cref="SpaceHeaterWindow"/> and updates it when new server messages are received.
/// </summary>
[UsedImplicitly]
public sealed class SpaceHeaterBoundUserInterface : BoundUserInterface
{

    [ViewVariables]
    private SpaceHeaterWindow? _window;

    public SpaceHeaterBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<SpaceHeaterWindow>();

        _window.ToggleStatusButton.OnToggled += _ => SendPredictedMessage(new SpaceHeaterToggleMessage());
        _window.IncreaseTempRange.OnPressed += _ => SendPredictedMessage(new SpaceHeaterChangeTemperatureMessage(_window.TemperatureChangeDelta));
        _window.DecreaseTempRange.OnPressed += _ => SendPredictedMessage(new SpaceHeaterChangeTemperatureMessage(-_window.TemperatureChangeDelta));
        _window.ModeSelector.OnItemSelected += args => SendPredictedMessage(new SpaceHeaterChangeModeMessage((SpaceHeaterMode)args.Id));
        _window.PowerLevelSelector.OnItemSelected += args => SendPredictedMessage(new SpaceHeaterChangePowerLevelMessage((SpaceHeaterPowerLevel)args.Id));

        Update();
    }

    public override void Update()
    {
        base.Update();

        if (_window == null
            || !EntMan.TryGetComponent(Owner, out GasThermoMachineComponent? gasThermoMachine)
            || !EntMan.TryGetComponent(Owner, out SpaceHeaterComponent? spaceHeaterComp))
            return;

        var isPowered = EntMan.TryGetComponent<ApcPowerReceiverComponent>(Owner, out var apcComp) && !apcComp.PowerDisabled;
        _window.ToggleStatusButton.Pressed = isPowered;
        _window.ModeSelector.SelectId((int)spaceHeaterComp.Mode);
        _window.PowerLevelSelector.Select((int)spaceHeaterComp.PowerLevel);

        _window.SetTemperature(gasThermoMachine.TargetTemperature, spaceHeaterComp.MinTemperature, spaceHeaterComp.MaxTemperature);
    }
}
