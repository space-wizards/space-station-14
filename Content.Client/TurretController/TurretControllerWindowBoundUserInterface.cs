using Content.Shared.Access;
using Content.Shared.TurretController;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.TurretController;

public sealed class TurretControllerWindowBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private TurretControllerWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<TurretControllerWindow>();
        _window.SetOwner(Owner);
        _window.OpenCentered();

        _window.OnAccessLevelsChangedEvent += OnAccessLevelChanged;
        _window.OnArmamentSettingChangedEvent += OnArmamentSettingChanged;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not DeployableTurretControllerBoundInterfaceState { } castState)
            return;

        _window?.UpdateState(castState);
    }

    private void OnAccessLevelChanged(HashSet<ProtoId<AccessLevelPrototype>> accessLevels, bool enabled)
    {
        SendPredictedMessage(new DeployableTurretExemptAccessLevelChangedMessage(accessLevels, enabled));
    }

    private void OnArmamentSettingChanged(TurretControllerWindow.TurretArmamentSetting setting)
    {
        SendPredictedMessage(new DeployableTurretArmamentSettingChangedMessage((int)setting));
    }
}
