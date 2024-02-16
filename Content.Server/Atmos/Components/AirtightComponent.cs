using Content.Shared.Atmos;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Atmos.Components
{
    [RegisterComponent]
    public sealed partial class AirtightComponent : Component
    {
        public (EntityUid Grid, Vector2i Tile) LastPosition { get; set; }

        [DataField("airBlockedDirection", customTypeSerializer: typeof(FlagSerializer<AtmosDirectionFlags>))]
        public int InitialAirBlockedDirection { get; set; } = (int) AtmosDirection.All;

        [ViewVariables]
        public int CurrentAirBlockedDirection;

        [DataField("airBlocked")]
        public bool AirBlocked { get; set; } = true;

        [DataField("fixVacuum")]
        public bool FixVacuum { get; set; } = true;

        [DataField("rotateAirBlocked")]
        public bool RotateAirBlocked { get; set; } = true;

        [DataField("fixAirBlockedDirectionInitialize")]
        public bool FixAirBlockedDirectionInitialize { get; set; } = true;

        [DataField("noAirWhenFullyAirBlocked")]
        public bool NoAirWhenFullyAirBlocked { get; set; } = true;

        public AtmosDirection AirBlockedDirection => (AtmosDirection)CurrentAirBlockedDirection;
    }
}
