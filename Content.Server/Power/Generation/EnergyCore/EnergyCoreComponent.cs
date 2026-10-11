using Content.Shared.Atmos;
using Content.Shared.Materials;
using Content.Shared.Power.Generation.EnergyCore;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Power.Generation.EnergyCore;

[RegisterComponent, AutoGenerateComponentPause, Access(typeof(EnergyCoreSystem))]
public sealed partial class EnergyCoreComponent : Component
{
    [DataField]
    public float MaxSupply = 300000;

    [DataField]
    public float TargetSupply = 100000;

    [DataField]
    public float MinimumOperatingSupply = 10000;

    [DataField]
    public float NominalEfficiency = 0.35f;

    [DataField]
    public float CycleEfficiency = 0.65f;

    [DataField]
    public TimeSpan ReactionTime = TimeSpan.FromSeconds(2);

    [DataField]
    public float FeedbackTemperature = 900;

    [DataField]
    public float TemperatureFeedback = 0.005f;

    [DataField]
    public float FissionPower;

    [DataField]
    public float DecayFraction = 0.06f;

    [DataField]
    public TimeSpan DecayTime = TimeSpan.FromSeconds(30);

    [DataField]
    public float DecayEnergy;

    [DataField]
    public float ElectricalEnergy;

    [DataField]
    public float ElectricalCapacity = 600000;

    [ViewVariables]
    public float PendingElectricalEnergy;

    [ViewVariables]
    public float ConversionEfficiency;

    [ViewVariables]
    public float DecayHeat;

    [DataField]
    public ProtoId<MaterialPrototype> FuelMaterial = "Uranium";

    // 100 material units/sheet * 171428.58 J/unit * 0.35 = 6 MJ electrical energy/sheet.
    [DataField]
    public float FuelEnergyPerUnit = 171428.58f;

    [DataField]
    public float FuelEnergyRemaining;

    [DataField]
    public float IdleRadiation = 0;

    [DataField]
    public float ActiveRadiation = 8;

    [DataField]
    public float Temperature = Atmospherics.T20C;

    [DataField]
    public float HeatCapacity = 20000;

    [DataField]
    public float Conductance = 1600;

    [DataField]
    public float ShutdownTemperature = 1100;

    [DataField]
    public float RestartTemperature = 650;

    [DataField]
    public float FlowRate = 200;

    [DataField]
    public float PressureHead = 3000;

    [DataField]
    public string Inlet = "inlet";

    [DataField]
    public string Outlet = "outlet";

    [DataField]
    public TimeSpan StartupTime = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan ShutdownTime = TimeSpan.FromSeconds(4);

    [DataField]
    public EnergyCoreState State;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan TransitionEnd;

    [ViewVariables]
    public float HeatOutput;

    [ViewVariables]
    public float CoolantFlow;
}
