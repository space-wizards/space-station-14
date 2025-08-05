using Content.Shared.Implants.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Implants
{
    /// <summary>
    /// Component for USSP uplink implants that need to be linked to head revolutionaries.
    /// </summary>
    [RegisterComponent]
    public sealed partial class USSPUplinkImplantComponent : Component
    {
        // This component is just a marker for the USSP uplink implant
        // so we can subscribe to the ImplantImplantedEvent for this specific component
    }
}
