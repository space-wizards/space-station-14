using Content.Shared.CCVar;

namespace Content.Server.Atmos.EntitySystems
{
    public partial class AtmosphereSystem
    {
        public bool SpaceWind { get; private set; }
        public bool MonstermosEqualization { get; private set; }
        public bool Superconduction { get; private set; }
        public bool ExcitedGroupsSpaceIsAllConsuming { get; private set; }
        public float AtmosMaxProcessTime { get; private set; }
        public float AtmosTickRate { get; private set; }

        private void InitializeCVars()
        {
            _cfg.OnValueChanged(CCVars.SpaceWind, value => SpaceWind = value, true);
            _cfg.OnValueChanged(CCVars.MonstermosEqualization, value => MonstermosEqualization = value, true);
            _cfg.OnValueChanged(CCVars.Superconduction, value => Superconduction = value, true);
            _cfg.OnValueChanged(CCVars.AtmosMaxProcessTime, value => AtmosMaxProcessTime = value, true);
            _cfg.OnValueChanged(CCVars.AtmosTickRate, value => AtmosTickRate = value, true);
            _cfg.OnValueChanged(CCVars.ExcitedGroupsSpaceIsAllConsuming, value => ExcitedGroupsSpaceIsAllConsuming = value, true);
        }
    }
}
