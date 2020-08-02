using System;
using System.Threading;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Server.GameObjects.Components.Power.PowerNetComponents
{
    [RegisterComponent]
    public class RadiationPanel : PowerSupplierComponent, IInteractHand
    {
        public override string Name => "RadiationPanel";

        private int _radiation;
        private bool enabled;

        private CancellationTokenSource tokenSource;

        public int Radiation
        {
            get
            {
                return _radiation;
            }
            set
            {
                _radiation = Math.Clamp(value, 0, 2000);
                if (_radiation >= 100 && enabled)
                {
                    SupplyRate = _radiation;
                }
                else
                {
                    SupplyRate = 0;
                }
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            tokenSource = new CancellationTokenSource();

        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            if (!enabled)
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("You turn on the radiation panel."));
                enabled = true;
            }
            else
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("You turn off the radiation panel."));
                enabled = false;
            }

            return true;
        }

        void EnableCollection()
        {
            enabled = true;
            SupplyRate = _radiation;
            Timer.SpawnRepeating(1000, () =>
            {
                Radiation -= Math.Clamp(Radiation / 10, 10, int.MaxValue);
            }, tokenSource.Token);
        }

        void DisableCollection()
        {
            enabled = false;
            tokenSource.Cancel();
        }
    }
}
