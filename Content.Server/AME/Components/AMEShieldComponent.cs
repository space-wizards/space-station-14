using Content.Shared.AME;
using Robust.Server.GameObjects;

namespace Content.Server.AME.Components
{
    [RegisterComponent]
    public sealed class AMEShieldComponent : SharedAMEShieldComponent
    {

        private bool _isCore = false;

        [ViewVariables]
        public int CoreIntegrity = 100;

        private AppearanceComponent? _appearance;
        private PointLightComponent? _pointLight;

        protected override void Initialize()
        {
            base.Initialize();
            var entMan = IoCManager.Resolve<IEntityManager>();
            entMan.TryGetComponent(Owner, out _appearance);
            entMan.TryGetComponent(Owner, out _pointLight);
        }

        public void SetCore()
        {
            if(_isCore) { return; }
            _isCore = true;
            if (_appearance != null)
                IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SharedAppearanceSystem>()
                    .SetData(Owner, AMEShieldVisuals.Core, "isCore", _appearance);
        }

        public void UnsetCore()
        {
            _isCore = false;
            if (_appearance != null)
                IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SharedAppearanceSystem>()
                    .SetData(Owner, AMEShieldVisuals.Core, "isNotCore", _appearance);
            UpdateCoreVisuals(0, false);
        }

        public void UpdateCoreVisuals(int injectionStrength, bool injecting)
        {
            if (!injecting)
            {
                if (_appearance != null)
                    IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SharedAppearanceSystem>()
                        .SetData(Owner, AMEShieldVisuals.CoreState, "off", _appearance);
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
                if (_appearance != null)
                    IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SharedAppearanceSystem>()
                        .SetData(Owner, AMEShieldVisuals.CoreState, "strong", _appearance);
                return;
            }

            if (_appearance != null)
                IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SharedAppearanceSystem>()
                    .SetData(Owner, AMEShieldVisuals.CoreState, "weak", _appearance);
        }
    }
}
