using Content.Shared.GameObjects.Components.Power.AME;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Server.GameObjects.Components.Power.AME
{
    [RegisterComponent]
    public class AMEShieldComponent : SharedAMEShieldComponent
    {

        private bool _isCore = false;

        [ViewVariables]
        public int CoreIntegrity = 100;

        private AppearanceComponent _appearance;

        public override void Initialize()
        {
            base.Initialize();
            Owner.TryGetComponent(out _appearance);
        }

        internal void OnUpdate(float frameTime)
        {
            throw new NotImplementedException();
        }

        public void SetCore()
        {
            if(_isCore) { return; }
            _isCore = true;
            _appearance.SetData(AMEShieldVisuals.Core, "isCore");
        }

        public void UnsetCore()
        {
            _isCore = false;
            _appearance.SetData(AMEShieldVisuals.Core, "isNotCore");
        }

        public void UpdateCoreVisuals(int injectionStrength, bool injecting)
        {
            if (!injecting)
            {
                _appearance.SetData(AMEShieldVisuals.CoreState, "off");
                return;
            }

            if (injectionStrength > 2)
            {
                _appearance.SetData(AMEShieldVisuals.CoreState, "strong");
                return;
            }

            _appearance.SetData(AMEShieldVisuals.CoreState, "weak");

        }
    }
}
