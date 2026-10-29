using Content.Shared.Atmos.Piping.Portable.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Atmos.UI;

/// <summary>
/// A BUI for space heaters, wraps a <see cref="SpaceHeaterWindow"/>
/// </summary>
/// <seealso cref="SpaceHeaterComponent"/>
[UsedImplicitly]
public sealed class SpaceHeaterBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private SpaceHeaterWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<SpaceHeaterWindow>();

        _window.ToggleStatusButton.OnToggled += _ => OnToggleStatusButtonPressed();
        _window.IncreaseTempRange.OnPressed += _ => OnTemperatureRangeChanged(_window.TemperatureChangeDelta);
        _window.DecreaseTempRange.OnPressed += _ => OnTemperatureRangeChanged(-_window.TemperatureChangeDelta);
        _window.ModeSelector.OnItemSelected += OnModeChanged;

        _window.PowerLevelSelector.OnItemSelected += OnPowerLevelChange;
    }

    private void OnToggleStatusButtonPressed()
    {
        SendMessage(new SpaceHeaterToggleMessage());
    }

    private void OnTemperatureRangeChanged(float changeAmount)
    {
        SendMessage(new SpaceHeaterChangeTemperatureMessage(changeAmount));
    }

    private void OnModeChanged(OptionButton.ItemSelectedEventArgs args)
    {
        _window?.ModeSelector.SelectId(args.Id);
        SendMessage(new SpaceHeaterChangeModeMessage((SpaceHeaterMode)args.Id));
    }

    private void OnPowerLevelChange(RadioOptionItemSelectedEventArgs<int> args)
    {
        _window?.PowerLevelSelector.Select(args.Id);
        SendMessage(new SpaceHeaterChangePowerLevelMessage((SpaceHeaterPowerLevel)args.Id));
    }

    /// <summary>
    ///     Update the UI state based on server-sent info
    /// </summary>
    /// <param name="state"></param>
    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (_window == null || state is not SpaceHeaterBoundUserInterfaceState cast)
            return;

        _window.SetActive(cast.Enabled);
        _window.ModeSelector.SelectId((int)cast.Mode);
        _window.PowerLevelSelector.Select((int)cast.PowerLevel);

        _window.MinTemp = cast.MinTemperature;
        _window.MaxTemp = cast.MaxTemperature;
        _window.SetTemperature(cast.TargetTemperature);
    }
}
