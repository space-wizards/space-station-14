using Content.Server.Power.EntitySystems;

namespace Content.Server.Power.Components
{
    /// <summary>
    ///     Battery node on the pow3r network. Needs other components to connect to actual networks.
    /// </summary>
    [RegisterComponent]
    [Virtual]
    public partial class BatteryComponent : Component
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
        public string SolutionName = "battery";

        /// <summary>
        /// Maximum charge of the battery in joules (ie. watt seconds)
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float MaxCharge
        {
            get => _maxCharge;
            [Obsolete("Use system method")]
            set => _entMan.System<BatterySystem>().SetMaxCharge(Owner, value, this);
        }

        [DataField("maxCharge")]
        [Access(typeof(BatterySystem))]
        public float _maxCharge;

        /// <summary>
        /// Current charge of the battery in joules (ie. watt seconds)
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float CurrentCharge
        {
            get => Charge;
            [Obsolete("Use system method")]
            set => _entMan.System<BatterySystem>().SetCharge(Owner, value, this);
        }

        [DataField("startingCharge")]
        [Access(typeof(BatterySystem))]
        public float Charge;

        /// <summary>
        /// True if the battery is fully charged.
        /// </summary>
        [ViewVariables] public bool IsFullyCharged => MathHelper.CloseToPercent(CurrentCharge, MaxCharge);

        /// <summary>
        /// The price per one joule. Default is 1 credit for 10kJ.
        /// </summary>
        [DataField("pricePerJoule")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float PricePerJoule = 0.0001f;

        [Obsolete("Use system method")]
        public bool TryUseCharge(float value)
            => _entMan.System<BatterySystem>().TryUseCharge(Owner, value, this);
    }

    /// <summary>
    ///     Raised when a battery's charge or capacity changes (capacity affects relative charge percentage).
    /// </summary>
    [ByRefEvent]
    public readonly record struct ChargeChangedEvent(float Charge, float MaxCharge);
}
