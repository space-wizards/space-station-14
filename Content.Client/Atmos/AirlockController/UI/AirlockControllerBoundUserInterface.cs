using Content.Shared.Atmos.AirlockController;
using Robust.Client.Player;
using Robust.Client.UserInterface;

namespace Content.Client.Atmos.AirlockController.UI;

public sealed partial class AirlockControllerBoundUserInterface : BoundUserInterface
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private AirlockControllerSystem _controller = default!;

    private AirlockControllerWindow? _window;

    public AirlockControllerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<AirlockControllerWindow>();
        _window.SetEntity(Owner);

        _window.CycleRequested += side => SendMessage(new AirlockControllerCycleMessage(side));
        _window.CancelRequested += () => SendMessage(new AirlockControllerCancelMessage());

        // Access locked config (Atmos)
        _window.ConfigRequested += () => SendMessage(new AirlockControllerOpenConfigMessage());
        if (_player.LocalEntity is { } player)
            _window.SetConfigAllowed(_controller.IsAllowedQuiet(player, Owner));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is AirlockControllerUiState cast)
            _window?.UpdateState(cast);
    }
}

public sealed class AirlockControllerConfigBoundUserInterface : BoundUserInterface
{
    private AirlockControllerConfigWindow? _window;

    public AirlockControllerConfigBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<AirlockControllerConfigWindow>();

        // Predicted magic
        _window.VentRolesChanged += (uid, roles) => SendPredictedMessage(new AirlockControllerSetVentRolesMessage(uid, roles));
        _window.DoorSideChanged += (uid, side) => SendPredictedMessage(new AirlockControllerSetDoorSideMessage(uid, side));
        _window.CyclerSideChanged += (uid, side) => SendPredictedMessage(new AirlockControllerSetCyclerSideMessage(uid, side));
        _window.TargetSensorChanged += (side, uid) => SendPredictedMessage(new AirlockControllerSetTargetSensorMessage(side, uid));
        _window.PresetChanged += (side, pressure) => SendPredictedMessage(new AirlockControllerSetPresetMessage(side, pressure));
        _window.MaintenanceChanged += enabled => SendPredictedMessage(new AirlockControllerSetMaintenanceMessage(enabled));

        _window.ForceSideRequested += side => SendMessage(new AirlockControllerForceSideMessage(side));

        Update();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is AirlockControllerConfigUiState cast)
            _window?.SetTelemetry(cast);
    }

    public override void Update()
    {
        if (_window != null && EntMan.TryGetComponent(Owner, out AirlockControllerComponent? controller))
            _window.SetConfig(controller);
    }
}
