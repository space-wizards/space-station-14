using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos.Components
{
    [RegisterComponent]
    public sealed class AirtightComponent : Component
    {
        public (GridId Grid, Vector2i Tile) LastPosition { get; set; }

        [DataField("airBlockedDirection", customTypeSerializer: typeof(FlagSerializer<AtmosDirectionFlags>))]
        [ViewVariables]
        public int InitialAirBlockedDirection { get; set; } = (int) AtmosDirection.All;

        [ViewVariables]
        public int CurrentAirBlockedDirection;

        [DataField("airBlocked")]
        public bool AirBlocked { get; set; } = true;

        [DataField("fixVacuum")]
        public bool FixVacuum { get; set; } = true;

        [ViewVariables]
        [DataField("rotateAirBlocked")]
        public bool RotateAirBlocked { get; set; } = true;

        [ViewVariables]
        [DataField("fixAirBlockedDirectionInitialize")]
        public bool FixAirBlockedDirectionInitialize { get; set; } = true;

        [ViewVariables]
        [DataField("noAirWhenFullyAirBlocked")]
        public bool NoAirWhenFullyAirBlocked { get; set; } = true;

        public AtmosDirection AirBlockedDirection => (AtmosDirection)CurrentAirBlockedDirection;
    }
}
