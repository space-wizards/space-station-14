using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Content.Shared.RussStation.Botany.Systems;

namespace Content.Shared.RussStation.Botany.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedSeedExtractorStorageSystem))]
public sealed partial class SeedExtractorStorageComponent : Component
{
    /// <summary>
    /// The ID of the container used to store seed packets placed inside the extractor.
    /// </summary>
    [DataField]
    public string SeedContainerId = "seed_extractor_seeds";

    /// <summary>
    /// Whitelist controlling which items the extractor will accept.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;
}
