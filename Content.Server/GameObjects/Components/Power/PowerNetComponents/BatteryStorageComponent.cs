using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Power.PowerNetComponents
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

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _activeDrawRate, "activeDrawRate", 100);
        }

        public override void Initialize()
        {
            base.Initialize();

            _battery = Owner.EnsureComponent<BatteryComponent>();
            Consumer = Owner.EnsureComponent<PowerConsumerComponent>();
            UpdateDrawRate();
        }

        public void Update(float frameTime)
        {
            //Simplified implementation - If a frame adds more power to a partially full battery than it can hold, the power is lost.
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
            if (Consumer.DrawRate != newConsumerDrawRate)
            {
                Consumer.DrawRate = newConsumerDrawRate;
            }
        }

        private void SetActiveDrawRate(int newEnabledDrawRate)
        {
            _activeDrawRate = newEnabledDrawRate;
            UpdateDrawRate();
        }
    }
}
