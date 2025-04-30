using Content.Client.UserInterface;
using Content.Client.UserInterface.Controls;
using Content.Shared.Chemistry.Components;
using Content.Shared.Power.EntitySystems;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Chemistry.UI.Hotplate;

[UsedImplicitly]
public sealed class HotplateBoundUserInterface : BoundUserInterface, IBuiPreTickUpdate
{
    private readonly SharedPowerReceiverSystem _power;

    [ViewVariables]
    private HotplateMenu? _window;

    private InputCoalescer<HotplateMode> _modeCoalescer;

    private float _lastSentSetpoint;
    private float _lastDisplayedTemperature;
    private HotplateActiveState? _lastDisplayedState;
    private bool _lastDisplayedBeakerPresent;

    public HotplateBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);

        _power = EntMan.System<SharedPowerReceiverSystem>();
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<HotplateMenu>();
        _window.OpenCentered();
        _window.SetInfoFromEntity(EntMan, Owner);

        _window.OnPowerChanged += powered => SendPredictedMessage(new HotplatePowerChangedMessage(powered));
        _window.OnSetpointChanged += setpoint => SendPredictedMessage(new HotplateSetpointChangedMessage(setpoint));
        // Sliders can't use predicted messsages and I don't know how to solve this with compstates
        _window.OnModeChanged += mode => _modeCoalescer.Set(mode);

        _window.SetPowered(_power.IsPowered(Owner));

        if (EntMan.TryGetComponent(Owner, out HotplateComponent? hotplate))
        {
            _window.SetTemperatureLimits(hotplate.MinTemperature, hotplate.MaxTemperature);

            _lastSentSetpoint = hotplate.Setpoint;
            _lastDisplayedTemperature = hotplate.CurrentTemperature;
            _lastDisplayedState = hotplate.ActiveState;
            _lastDisplayedBeakerPresent = hotplate.HasBeaker;

            _window.SetCurrentTemperature(hotplate.CurrentTemperature);
            _window.SetSetpoint(hotplate.Setpoint);
            _window.SetMode(hotplate.Mode);
            _window.SetBeakerPresent(hotplate.HasBeaker);
            _window.SetHysteresis(hotplate.Hysteresis);
            _window.UpdateStatusIndicators(hotplate.ActiveState);
        }
    }

    // void IBuiPreTickUpdate.PreTickUpdate()
    // {
    //     if (_modeCoalescer.CheckIsModified(out var modeValue))
    //         SendMessage(new HotplateModeChangedMessage(modeValue));
    // }
    //
    // public override void Update()
    // {
    //     if (_window == null)
    //         return;
    //
    //     _window.SetPowered(_power.IsPowered(Owner));
    //
    //     // Update component values if component exists
    //     if (EntMan.TryGetComponent(Owner, out HotplateComponent? hotplate))
    //     {
    //         _window.SetCurrentTemperature(hotplate.CurrentTemperature);
    //         _window.SetTemperatureLimits(hotplate.MinTemperature, hotplate.MaxTemperature);
    //         _window.SetSetpoint(hotplate.Setpoint);
    //         _window.SetMode(hotplate.Mode);
    //         _window.SetBeakerPresent(hotplate.HasBeaker);
    //         _window.UpdateStatusIndicators(hotplate.ActiveState);
    //     }
    // }

    // THIS IS FOR SURE NOT HOW IT'S SUPPOSED TO BE USED
    public void PreTickUpdate()
    {
        if (_window == null)
            return;

        // Only send mode change message once per tick when it's been modified
        if (_modeCoalescer.CheckIsModified(out var modeValue))
        {
            SendMessage(new HotplateModeChangedMessage(modeValue));
        }

        // Update power state every tick - this is important for responsiveness
        var isPowered = _power.IsPowered(Owner);
        _window.SetPowered(isPowered);

        // Only update temperature display and status indicators at intervals
        // but update other controls immediately
        if (EntMan.TryGetComponent(Owner, out HotplateComponent? hotplate))
        {
            // Only update temperature display if it has changed significantly (0.1K change)
            if (Math.Abs(_lastDisplayedTemperature - hotplate.CurrentTemperature) >= 0.1f)
            {
                _lastDisplayedTemperature = hotplate.CurrentTemperature;
                _window.SetCurrentTemperature(hotplate.CurrentTemperature);
            }

            // Update status indicators if state has changed
            if (_lastDisplayedState != hotplate.ActiveState)
            {
                _lastDisplayedState = hotplate.ActiveState;
                _window.UpdateStatusIndicators(hotplate.ActiveState);
            }

            // Update beaker presence if changed
            if (_lastDisplayedBeakerPresent != hotplate.HasBeaker)
            {
                _lastDisplayedBeakerPresent = hotplate.HasBeaker;
                _window.SetBeakerPresent(hotplate.HasBeaker);
            }
        }

    }

    public override void Update()
    {
        if (_window == null)
            return;

        // The PreTickUpdate handles continuous UI updates now
        // This method will only handle component property synchronization
        if (EntMan.TryGetComponent(Owner, out HotplateComponent? hotplate))
        {
            // Only update setpoint from server if it doesn't match what the client sent
            // This prevents the server from overriding user input during client prediction
            if (!MathHelper.CloseTo(_lastSentSetpoint, hotplate.Setpoint, 0.01f))
            {
                _lastSentSetpoint = hotplate.Setpoint;
                _window.SetSetpoint(hotplate.Setpoint);
            }

            // Mode is handled via the coalescer, so we only update it when not being controlled
            _window.SetMode(hotplate.Mode);
        }
    }
}
