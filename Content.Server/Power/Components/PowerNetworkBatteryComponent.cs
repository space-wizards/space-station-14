using Content.Server.Power.Pow3r;
using Content.Shared.Guidebook;

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
    public sealed partial class PowerNetworkBatteryComponent : Component
    {
        [ViewVariables] public float LastSupply = 0f;

        [DataField]
        [ViewVariables(VVAccess.ReadWrite)]
        public float MaxChargeRate
        {
            get => NetworkBattery.MaxChargeRate;
            set => NetworkBattery.MaxChargeRate = value;
        }

        [DataField]
        [ViewVariables(VVAccess.ReadWrite)]
        [GuidebookData]
        public float MaxSupply
        {
            get => NetworkBattery.MaxSupply;
            set => NetworkBattery.MaxSupply = value;
        }

        [DataField]
        [ViewVariables(VVAccess.ReadWrite)]
        public float SupplyRampTolerance
        {
            get => NetworkBattery.SupplyRampTolerance;
            set => NetworkBattery.SupplyRampTolerance = value;
        }

        [DataField]
        [ViewVariables(VVAccess.ReadWrite)]
        public float SupplyRampRate
        {
            get => NetworkBattery.SupplyRampRate;
            set => NetworkBattery.SupplyRampRate = value;
        }

        [DataField]
        [ViewVariables(VVAccess.ReadWrite)]
        public float SupplyRampPosition
        {
            get => NetworkBattery.SupplyRampPosition;
            set => NetworkBattery.SupplyRampPosition = value;
        }

        [DataField]
        [ViewVariables(VVAccess.ReadWrite)]
        public float CurrentSupply
        {
            get => NetworkBattery.CurrentSupply;
            set => NetworkBattery.CurrentSupply = value;
        }

        [DataField]
        [ViewVariables(VVAccess.ReadWrite)]
        public float CurrentReceiving
        {
            get => NetworkBattery.CurrentReceiving;
            set => NetworkBattery.CurrentReceiving = value;
        }

        [DataField]
        [ViewVariables(VVAccess.ReadWrite)]
        public float LoadingNetworkDemand
        {
            get => NetworkBattery.LoadingNetworkDemand;
            set => NetworkBattery.LoadingNetworkDemand = value;
        }

        [DataField]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled
        {
            get => NetworkBattery.Enabled;
            set => NetworkBattery.Enabled = value;
        }

        [DataField]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanCharge
        {
            get => NetworkBattery.CanCharge;
            set => NetworkBattery.CanCharge = value;
        }

        [DataField]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanDischarge
        {
            get => NetworkBattery.CanDischarge;
            set => NetworkBattery.CanDischarge = value;
        }

        [DataField]
        [ViewVariables(VVAccess.ReadWrite)]
        public float Efficiency
        {
            get => NetworkBattery.Efficiency;
            set => NetworkBattery.Efficiency = value;
        }

        [ViewVariables]
        public PowerState.Battery NetworkBattery { get; } = new();
    }
}
