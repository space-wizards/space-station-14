using Robust.Shared.Prototypes;

namespace Content.Server.Atmos.Components
{
    /// <summary>
    /// Used in test maps to resolve coordinates
    /// </summary>
    [RegisterComponent, EntityCategory("Mapping")]
    public sealed partial class IntegrationTestMarkerComponent : Component
    {
        // Set during mapping to look up in test runtime
        [DataField("name")]
        public string Name { get; private set; } = default!;
    }
}
