using Content.Shared.CCVar;
using Robust.Shared.Configuration;

namespace Content.Server.Atmos.EntitySystems
{
    public sealed partial class AtmosphereSystem
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        public bool SpaceWind { get; private set; }
        public float SpaceWindPressureForceDivisorThrow { get; private set; }
        public float SpaceWindPressureForceDivisorPush { get; private set; }
        public float SpaceWindMaxVelocity { get; private set; }
        public float SpaceWindMaxPushForce { get; private set; }
        public bool MonstermosEqualization { get; private set; }
        public bool MonstermosDepressurization { get; private set; }
        public bool MonstermosRipTiles { get; private set; }
        public bool GridImpulse { get; private set; }
        public float SpacingEscapeRatio { get; private set; }
        public float SpacingMinGas { get; private set; }
        public float SpacingMaxWind { get; private set; }
        public bool Superconduction { get; private set; }
        public bool ExcitedGroups { get; private set; }
        public bool ExcitedGroupsSpaceIsAllConsuming { get; private set; }
        public float AtmosMaxProcessTime { get; private set; }
        public float AtmosTickRate { get; private set; }

        /// <summary>
        /// Time between each atmos sub-update.  If you are writing an atmos device, use AtmosDeviceUpdateEvent.dt
        /// instead of this value, because atmos devices do not update each are sub-update and sometimes are skipped to
        /// meet the tick deadline.
        /// </summary>
        public float AtmosTime => 1f / AtmosTickRate;

        private void InitializeCVars()
        {
            _cfg.OnValueChanged(CCVars.SpaceWind, value => SpaceWind = value, true);
            _cfg.OnValueChanged(CCVars.SpaceWindPressureForceDivisorThrow, value => SpaceWindPressureForceDivisorThrow = value, true);
            _cfg.OnValueChanged(CCVars.SpaceWindPressureForceDivisorPush, value => SpaceWindPressureForceDivisorPush = value, true);
            _cfg.OnValueChanged(CCVars.SpaceWindMaxVelocity, value => SpaceWindMaxVelocity = value, true);
            _cfg.OnValueChanged(CCVars.SpaceWindMaxPushForce, value => SpaceWindMaxPushForce = value, true);
            _cfg.OnValueChanged(CCVars.MonstermosEqualization, value => MonstermosEqualization = value, true);
            _cfg.OnValueChanged(CCVars.MonstermosDepressurization, value => MonstermosDepressurization = value, true);
            _cfg.OnValueChanged(CCVars.MonstermosRipTiles, value => MonstermosRipTiles = value, true);
            _cfg.OnValueChanged(CCVars.AtmosGridImpulse, value => GridImpulse = value, true);
            _cfg.OnValueChanged(CCVars.AtmosSpacingEscapeRatio, value => SpacingEscapeRatio = value, true);
            _cfg.OnValueChanged(CCVars.AtmosSpacingMinGas, value => SpacingMinGas = value, true);
            _cfg.OnValueChanged(CCVars.AtmosSpacingMaxWind, value => SpacingMaxWind = value, true);
            _cfg.OnValueChanged(CCVars.Superconduction, value => Superconduction = value, true);
            _cfg.OnValueChanged(CCVars.AtmosMaxProcessTime, value => AtmosMaxProcessTime = value, true);
            _cfg.OnValueChanged(CCVars.AtmosTickRate, value => AtmosTickRate = value, true);
            _cfg.OnValueChanged(CCVars.ExcitedGroups, value => ExcitedGroups = value, true);
            _cfg.OnValueChanged(CCVars.ExcitedGroupsSpaceIsAllConsuming, value => ExcitedGroupsSpaceIsAllConsuming = value, true);
        }
    }
}
