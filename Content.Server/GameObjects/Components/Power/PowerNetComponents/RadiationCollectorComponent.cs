#nullable enable
using System;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Singularity;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Physics;
using Robust.Shared.Timing;

namespace Content.Server.GameObjects.Components.Power.PowerNetComponents
{
    [RegisterComponent]
    public class RadiationCollectorComponent : PowerSupplierComponent, IInteractHand, IRadiationAct
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override string Name => "RadiationCollector";
        private bool _enabled;
        private TimeSpan _coolDownEnd;

        [ComponentDependency] private readonly PhysicsComponent? _collidableComponent = default!;

        public void OnAnchoredChanged()
        {
            if(_collidableComponent != null && _collidableComponent.BodyType == BodyType.Static)
                Owner.SnapToGrid();
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

        void IRadiationAct.RadiationAct(float frameTime, SharedRadiationPulseComponent radiation)
        {
            if (!_enabled) return;

            SupplyRate = (int) (frameTime * radiation.RadsPerSecond * 3000f);
        }

        protected void SetAppearance(RadiationCollectorVisualState state)
        {
            if (Owner.TryGetComponent<AppearanceComponent>(out var appearance))
            {
                appearance.SetData(RadiationCollectorVisuals.VisualState, state);
            }
        }
    }
}
