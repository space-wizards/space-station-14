namespace Content.Server.Power.Components
{
    /// <summary>
    ///     Battery node on the pow3r network. Needs other components to connect to actual networks.
    /// </summary>
    [RegisterComponent]
    [Virtual]
    public class BatteryComponent : Component
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        /// <summary>
        /// Maximum charge of the battery in joules (ie. watt seconds)
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] public double MaxCharge { get => _maxCharge; set => SetMaxCharge(value); }
        [DataField("maxCharge")]
        private double _maxCharge;

        /// <summary>
        /// Current charge of the battery in joules (ie. watt seconds)
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public double CurrentCharge { get => _currentCharge; set => SetCurrentCharge(value); }
        [DataField("startingCharge")]
        private double _currentCharge;

        /// <summary>
        /// True if the battery is fully charged.
        /// </summary>
        [ViewVariables] public bool IsFullyCharged => MathHelper.CloseToPercent(CurrentCharge, MaxCharge);

        /// <summary>
        ///     If sufficient charge is avaiable on the battery, use it. Otherwise, don't.
        /// </summary>
        public virtual bool TryUseCharge(double chargeToUse)
        {
            if (chargeToUse >= CurrentCharge)
            {
                return false;
            }
            else
            {
                CurrentCharge -= chargeToUse;
                return true;
            }
        }

        public virtual double UseCharge(double toDeduct)
        {
            var chargeChangedBy = Math.Min(CurrentCharge, toDeduct);
            CurrentCharge -= chargeChangedBy;
            return chargeChangedBy;
        }

        public void FillFrom(BatteryComponent battery)
        {
            var powerDeficit = MaxCharge - CurrentCharge;
            if (battery.TryUseCharge(powerDeficit))
            {
                CurrentCharge += powerDeficit;
            }
            else
            {
                CurrentCharge += battery.CurrentCharge;
                battery.CurrentCharge = 0;
            }
        }

        protected virtual void OnChargeChanged()
        {
            _entMan.EventBus.RaiseLocalEvent(Owner, new ChargeChangedEvent(), false);
        }

        private void SetMaxCharge(double newMax)
        {
            _maxCharge = Math.Max(newMax, 0);
            _currentCharge = Math.Min(_currentCharge, MaxCharge);
            OnChargeChanged();
        }

        private void SetCurrentCharge(double newChargeAmount)
        {
            _currentCharge = MathHelper.Clamp(newChargeAmount, 0, MaxCharge);
            OnChargeChanged();
        }
    }

    public struct ChargeChangedEvent {}
}
