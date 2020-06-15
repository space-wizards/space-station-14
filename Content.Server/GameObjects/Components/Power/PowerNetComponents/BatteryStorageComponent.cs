using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.NewPower.PowerNetComponents
{
    /// <summary>
    ///     Takes power via a <see cref="PowerConsumerComponent"/> to charge a <see cref="BatteryComponent"/>.
    /// </summary>
    [RegisterComponent]
    public class BatteryStorageComponent : Component
    {
        public override string Name => "BatteryStorage";

        [ViewVariables(VVAccess.ReadWrite)]
        public int ActiveDrawRate { get => _activeDrawRate; set => SetActiveDrawRate(value); }
        private int _activeDrawRate;

        [ViewVariables]
        private BatteryComponent _battery;

        [ViewVariables]
        public PowerConsumerComponent Consumer { get; private set; }

        [ViewVariables]
        private int _consumerDrawRate = 0;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _activeDrawRate, "activeDrawRate", 100);
        }

        public override void Initialize()
        {
            base.Initialize();
            _battery = Owner.GetComponent<BatteryComponent>();
            Consumer = Owner.GetComponent<PowerConsumerComponent>();
            UpdateDrawRate();
        }

        public void Update(float frameTime)
        {
            //Simlified implementation - If a frame adds more power to a partially full battery than it can hold, the power is lost.
            _battery.CurrentCharge += Consumer.ReceivedPower * frameTime;
            UpdateDrawRate();
        }

        private void UpdateDrawRate()
        {
            if (_battery.BatteryState == BatteryState.Full)
            {
                SetConsumerDraw(0);
            }
            else
            {
                SetConsumerDraw(ActiveDrawRate);
            }
        }

        private void SetConsumerDraw(int newConsumerDrawRate)
        {
            _consumerDrawRate = newConsumerDrawRate;
            if (Consumer.DrawRate != _consumerDrawRate)
            {
                Consumer.DrawRate = _consumerDrawRate;
            }
        }

        private void SetActiveDrawRate(int newEnabledDrawRate)
        {
            _activeDrawRate = newEnabledDrawRate;
            UpdateDrawRate();
        }
    }
}
