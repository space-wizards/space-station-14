using Content.Shared.Power.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Power.Components
{
    /// <summary>
    ///     Battery node on the pow3r network. Needs other components to connect to actual networks.
    /// </summary>
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    [Virtual, Access(typeof(SharedBatterySystem))]
    public partial class BatteryComponent : Component
    {
        public string SolutionName = "battery";

        /// <summary>
        /// Maximum charge of the battery in joules (ie. watt seconds)
        /// </summary>
        [DataField, AutoNetworkedField]
        public float MaxCharge;

        /// <summary>
        /// Current charge of the battery in joules (ie. watt seconds)
        /// </summary>
        [DataField("startingCharge"), AutoNetworkedField]
        public float CurrentCharge;

        /// <summary>
        /// True if the battery is fully charged.
        /// </summary>
        [ViewVariables]
        public bool IsFullyCharged => MathHelper.CloseToPercent(CurrentCharge, MaxCharge);

        /// <summary>
        /// The price per one joule. Default is 1 credit for 10kJ.
        /// </summary>
        [DataField]
        public float PricePerJoule = 0.0001f;

        /// <summary>
        /// The earliest time that <see cref="BatterySystem.SetCharge"/> or <see cref="BatterySystem.SetCharge"/> can next cause a network state update.
        /// This is to avoid spamming state updates every tick, since charge level tends to change often.
        /// </summary>
        public TimeSpan NextSyncTime = TimeSpan.Zero;
    }

    /// <summary>
    ///     Raised when a battery's charge or capacity changes (capacity affects relative charge percentage).
    /// </summary>
    [ByRefEvent]
    public readonly record struct ChargeChangedEvent(float Charge, float MaxCharge);
}
