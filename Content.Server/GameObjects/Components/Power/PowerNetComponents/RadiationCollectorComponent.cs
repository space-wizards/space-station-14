using System;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Singularity;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;

namespace Content.Server.GameObjects.Components.Power.PowerNetComponents
{
    [RegisterComponent]
    public class RadiationCollectorComponent : PowerSupplierComponent, IInteractHand, IRadiationAct
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override string Name => "RadiationCollector";
        private bool _enabled;
        private TimeSpan _coolDownEnd;

        private PhysicsComponent _collidableComponent;

        public override void Initialize()
        {
            base.Initialize();
            if (!Owner.TryGetComponent(out _collidableComponent))
            {
                Logger.Error("RadiationCollectorComponent created with no CollidableComponent");
                return;
            }
        }

        public override void HandleMessage(ComponentMessage message, IComponent component)
        {
            base.HandleMessage(message, component);
            switch (message)
            {
                case AnchoredChangedMessage:
                    OnAnchoredChanged();
                    break;
            }
        }

        private void OnAnchoredChanged()
        {
            if(_collidableComponent.Anchored) Owner.SnapToGrid();
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            var curTime = _gameTiming.CurTime;

            if(curTime < _coolDownEnd)
                return true;

            if (!_enabled)
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("The collector turns on."));
                EnableCollection();
            }
            else
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("The collector turns off."));
                DisableCollection();
            }

            _coolDownEnd = curTime + TimeSpan.FromSeconds(0.81f);

            return true;
        }

        void EnableCollection()
        {
            _enabled = true;
            SetAppearance(RadiationCollectorVisualState.Activating);
        }

        void DisableCollection()
        {
            _enabled = false;
            SetAppearance(RadiationCollectorVisualState.Deactivating);
        }

        public void RadiationAct(SharedRadiationPulseComponent radiation, float ratio)
        {
            if (!_enabled) return;

            SupplyRate = (int) (ratio * radiation.Cooldown * radiation.Energy * 1000.0f);
        }

        protected void SetAppearance(RadiationCollectorVisualState state)
        {
            if (Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(RadiationCollectorVisuals.VisualState, state);
            }
        }
    }
}
