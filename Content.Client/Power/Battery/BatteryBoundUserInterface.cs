using Content.Client.UserInterface;
using Content.Shared.Power;
using JetBrains.Annotations;
using Robust.Client.Timing;
using Robust.Client.UserInterface;

namespace Content.Client.Power.Battery;

/// <summary>
/// BUI for <see cref="BatteryUiKey.Key"/>.
/// </summary>
/// <seealso cref="BoundUserInterfaceState"/>
/// <seealso cref="BatteryMenu"/>
[UsedImplicitly]
public sealed class BatteryBoundUserInterface : BoundUserInterface, IBuiPreTickUpdate
{
    [Dependency] private readonly IClientGameTiming _gameTiming = null!;

    [ViewVariables]
    private BatteryMenu? _menu;

    private BuiPredictionState? _pred;
    private InputCoalescer<float> _chargeRateCoalescer;
    private InputCoalescer<float> _dischargeRateCoalescer;

    public BatteryBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _pred = new BuiPredictionState(this, _gameTiming);

        _menu = this.CreateWindow<BatteryMenu>();
        _menu.SetEntity(Owner);

        _menu.OnInBreaker += val => _pred!.SendMessage(new BatterySetInputBreakerMessage(val));
        _menu.OnOutBreaker += val => _pred!.SendMessage(new BatterySetOutputBreakerMessage(val));

        _menu.OnChargeRate += val => _chargeRateCoalescer.Set(val);
        _menu.OnDischargeRate += val => _dischargeRateCoalescer.Set(val);
    }

    void IBuiPreTickUpdate.PreTickUpdate()
    {
        if (_chargeRateCoalescer.CheckIsModified(out var chargeRateValue))
            _pred!.SendMessage(new BatterySetChargeRateMessage(chargeRateValue));

        if (_dischargeRateCoalescer.CheckIsModified(out var dischargeRateValue))
            _pred!.SendMessage(new BatterySetDischargeRateMessage(dischargeRateValue));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not BatteryBuiState batteryState)
            return;

        foreach (var replayMsg in _pred!.MessagesToReplay())
        {
            switch (replayMsg)
            {
                case BatterySetInputBreakerMessage setInputBreaker:
                    batteryState.CanCharge = setInputBreaker.On;
                    break;

                case BatterySetOutputBreakerMessage setOutputBreaker:
                    batteryState.CanDischarge = setOutputBreaker.On;
                    break;

                case BatterySetChargeRateMessage setChargeRate:
                    batteryState.MaxChargeRate = setChargeRate.Rate;
                    break;

                case BatterySetDischargeRateMessage setDischargeRate:
                    batteryState.MaxSupply = setDischargeRate.Rate;
                    break;
            }
        }

        _menu?.Update(batteryState);
    }
}
