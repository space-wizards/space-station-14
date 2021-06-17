#nullable enable
using Content.Server.Battery.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Power.Components
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
        [DataField("activeDrawRate")]
        private int _activeDrawRate = 100;

        [ViewVariables]
        [ComponentDependency] private BatteryComponent? _battery = default!;

        [ViewVariables]
        public PowerConsumerComponent? Consumer => _consumer;

        [ComponentDependency] private PowerConsumerComponent? _consumer = default!;

        public override void Initialize()
        {
            base.Initialize();
            Owner.EnsureComponentWarn<PowerConsumerComponent>();
            UpdateDrawRate();
        }

        public void Update(float frameTime)
        {
            if (_consumer == null || _battery == null)
                return;

            //Simplified implementation - If a frame adds more power to a partially full battery than it can hold, the power is lost.
            _battery.CurrentCharge += _consumer.ReceivedPower * frameTime;
            UpdateDrawRate();
        }

        private void UpdateDrawRate()
        {
            if (_battery == null)
                return;

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
            if (_consumer == null)
                return;

            if (_consumer.DrawRate != newConsumerDrawRate)
            {
                _consumer.DrawRate = newConsumerDrawRate;
            }
        }

        private void SetActiveDrawRate(int newEnabledDrawRate)
        {
            _activeDrawRate = newEnabledDrawRate;
            UpdateDrawRate();
        }
    }
}
