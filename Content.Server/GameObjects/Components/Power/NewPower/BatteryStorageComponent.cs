using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.NewPower
{
    /// <summary>
    ///     Pulls power from a powernet to charge a <see cref="BatteryComponent"/> on its owner.
    /// </summary>
    [RegisterComponent]
    public class BatteryStorageComponent : Component
    {
        public override string Name => "BatteryStorage";

        /// <summary>
        ///     How much power this will attempt to draw if its battery is not full.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int EnabledDrawRate { get => _enabledDrawRate; set => SetEnabledDrawRate(value); }
        private int _enabledDrawRate;

        /// <summary>
        ///     The battery that this is trying to add power to.
        /// </summary>
        [ViewVariables]
        private BatteryComponent _battery;

        /// <summary>
        ///     The consumer power is being taken with.
        /// </summary>
        [ViewVariables]
        private PowerConsumerComponent _consumer;

        /// <summary>
        ///     Whether or not the battery is full, this will draw power
        ///     if the battery is not full.
        /// </summary>
        private bool _needsPower;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _enabledDrawRate, "enabledDrawRate", 100);
        }

        public override void Initialize()
        {
            base.Initialize();
            _battery = Owner.GetComponent<BatteryComponent>();
            _consumer = Owner.GetComponent<PowerConsumerComponent>();
            _needsPower = false; //starts out off, then updated to determine if it should be on
            UpdateDrawRate();
        }

        public void Update(float frameTime)
        {
            _battery.CurrentCharge += _consumer.ReceivedPower * frameTime;
            UpdateDrawRate();
        }

        private void UpdateDrawRate()
        {
            var newNeedsPower = _battery.BatteryState != BatteryState.Full; //this needs power if its battery is not full
            if (_needsPower != newNeedsPower) //if whether we needed power changed, update draw rate
            {
                _needsPower = newNeedsPower;
                if (_needsPower)
                {
                    _consumer.DrawRate = EnabledDrawRate;
                }
                else
                {
                    _consumer.DrawRate = 0;
                }
            }
        }

        private void SetEnabledDrawRate(int newEnabledDrawRate)
        {
            _enabledDrawRate = newEnabledDrawRate;
            if (_needsPower)
            {
                _consumer.DrawRate = EnabledDrawRate; //update draw if currently drawing
            }
        }
    }
}
