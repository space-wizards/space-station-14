using Content.Server.Power.Pow3r;

namespace Content.Server.Power.Components
{
    /// <summary>
    ///     Glue component that manages the pow3r network node for batteries that are connected to the power network.
    /// </summary>
    /// <remarks>
    ///     This needs components like <see cref="BatteryChargerComponent"/> to work correctly,
    ///     and battery storage should be handed off to components like <see cref="BatteryComponent"/>.
    /// </remarks>
    [RegisterComponent]
    public sealed class PowerNetworkBatteryComponent : Component
    {
        [ViewVariables] public double LastSupply = 0f;

        [DataField("maxChargeRate")]
        [ViewVariables(VVAccess.ReadWrite)]
        public double MaxChargeRate
        {
            get => NetworkBattery.MaxChargeRate;
            set => NetworkBattery.MaxChargeRate = value;
        }

        [DataField("maxSupply")]
        [ViewVariables(VVAccess.ReadWrite)]
        public double MaxSupply
        {
            get => NetworkBattery.MaxSupply;
            set => NetworkBattery.MaxSupply = value;
        }

        [DataField("supplyRampTolerance")]
        [ViewVariables(VVAccess.ReadWrite)]
        public double SupplyRampTolerance
        {
            get => NetworkBattery.SupplyRampTolerance;
            set => NetworkBattery.SupplyRampTolerance = value;
        }

        [DataField("supplyRampRate")]
        [ViewVariables(VVAccess.ReadWrite)]
        public double SupplyRampRate
        {
            get => NetworkBattery.SupplyRampRate;
            set => NetworkBattery.SupplyRampRate = value;
        }

        [DataField("supplyRampPosition")]
        [ViewVariables(VVAccess.ReadWrite)]
        public double SupplyRampPosition
        {
            get => NetworkBattery.SupplyRampPosition;
            set => NetworkBattery.SupplyRampPosition = value;
        }

        [DataField("currentSupply")]
        [ViewVariables(VVAccess.ReadWrite)]
        public double CurrentSupply
        {
            get => NetworkBattery.CurrentSupply;
            set => NetworkBattery.CurrentSupply = value;
        }

        [DataField("currentReceiving")]
        [ViewVariables(VVAccess.ReadWrite)]
        public double CurrentReceiving
        {
            get => NetworkBattery.CurrentReceiving;
            set => NetworkBattery.CurrentReceiving = value;
        }

        [DataField("loadingNetworkDemand")]
        [ViewVariables(VVAccess.ReadWrite)]
        public double LoadingNetworkDemand
        {
            get => NetworkBattery.LoadingNetworkDemand;
            set => NetworkBattery.LoadingNetworkDemand = value;
        }

        [DataField("enabled")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled
        {
            get => NetworkBattery.Enabled;
            set => NetworkBattery.Enabled = value;
        }

        [DataField("canCharge")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanCharge
        {
            get => NetworkBattery.CanCharge;
            set => NetworkBattery.CanCharge = value;
        }

        [DataField("canDisharge")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanDischarge
        {
            get => NetworkBattery.CanDischarge;
            set => NetworkBattery.CanDischarge = value;
        }

        [DataField("efficiency")]
        [ViewVariables(VVAccess.ReadWrite)]
        public double Efficiency
        {
            get => NetworkBattery.Efficiency;
            set => NetworkBattery.Efficiency = value;
        }

        [ViewVariables]
        public PowerState.Battery NetworkBattery { get; } = new();
    }
}
