using Content.Shared.Access;
using Content.Shared.TurretController;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.TurretController;

public sealed class TurretControllerWindowBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private TurretControllerWindow? _window;

    public TurretControllerWindowBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

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

    private void OnArmamentSettingChanged(int setting)
    {
        SendPredictedMessage(new DeployableTurretArmamentSettingChangedMessage(setting));
    }
}
