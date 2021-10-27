using System;
using Content.Shared.AME;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.AME.Components
{
    [RegisterComponent]
    public class AMEShieldComponent : SharedAMEShieldComponent
    {

        private bool _isCore = false;

        [ViewVariables]
        public int CoreIntegrity = 100;

        private AppearanceComponent? _appearance;
        private PointLightComponent? _pointLight;

        protected override void Initialize()
        {
            base.Initialize();
            Owner.TryGetComponent(out _appearance);
            Owner.TryGetComponent(out _pointLight);
        }

        public void SetCore()
        {
            if(_isCore) { return; }
            _isCore = true;
            _appearance?.SetData(AMEShieldVisuals.Core, "isCore");
        }

        public void UnsetCore()
        {
            _isCore = false;
            _appearance?.SetData(AMEShieldVisuals.Core, "isNotCore");
            UpdateCoreVisuals(0, false);
        }

        public void UpdateCoreVisuals(int injectionStrength, bool injecting)
        {
            if (!injecting)
            {
                _appearance?.SetData(AMEShieldVisuals.CoreState, "off");
                if (_pointLight != null) { _pointLight.Enabled = false; }
                return;
            }

            if (_pointLight != null)
            {
                _pointLight.Radius = Math.Clamp(injectionStrength, 1, 12);
                _pointLight.Enabled = true;
            }

            if (injectionStrength > 2)
            {
                _appearance?.SetData(AMEShieldVisuals.CoreState, "strong");
                return;
            }

            _appearance?.SetData(AMEShieldVisuals.CoreState, "weak");
        }
    }
}
