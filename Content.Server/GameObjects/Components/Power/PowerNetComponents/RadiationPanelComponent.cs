using System;
using System.Threading;
using Content.Shared.GameObjects.Components;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Server.GameObjects.Components.Power.PowerNetComponents
{
    [RegisterComponent]
    public class RadiationPanelComponent : PowerSupplierComponent, IInteractHand, IRadiationAct
    {
        public override string Name => "RadiationPanel";
        public void RadiationAct(float frameTime, SharedRadiationPulseComponent radiation)
        {
            throw new NotImplementedException();
        }

        private int _radiation;
        private bool _enabled;

        private CancellationTokenSource _tokenSource;

        public int Radiation
        {
            get => _radiation;
            set
            {
                _radiation = Math.Clamp(value, 0, 2000);
                if (_radiation >= 100 && _enabled)
                {
                    SupplyRate = _radiation;
                }
                else
                {
                    SupplyRate = 0;
                }
            }
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            if (!_enabled)
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("The panel turns on."));
                EnableCollection();
            }
            else
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("The panel turns off."));
                DisableCollection();
            }

            return true;
        }

        void EnableCollection()
        {
            _enabled = true;
            SupplyRate = _radiation;
            _tokenSource = new CancellationTokenSource();
            Timer.SpawnRepeating(1000, () =>
            {
                Radiation -= Math.Clamp(Radiation / 2, 100, int.MaxValue);
            }, _tokenSource.Token);
        }

        void DisableCollection()
        {
            _enabled = false;
            Radiation = 0;
            _tokenSource.Cancel();
        }
    }
}
