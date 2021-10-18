using Content.Server.Atmos.EntitySystems;
using Content.Server.Temperature.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos.Components
{
    /// <summary>
    /// Used by FixGridAtmos. Entities with this may get magically auto-deleted on map initialization in future.
    /// </summary>
    [RegisterComponent]
    public class AtmosFixMarkerComponent : Component
    {
        public override string Name => "AtmosFixMarker";

        // See FixGridAtmos for more details
        [DataField("mode")]
        public int Mode { get; set; } = 0;
    }
}
