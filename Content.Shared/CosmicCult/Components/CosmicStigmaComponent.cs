using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.CosmicCult.Components;

/// <summary>
/// Component for the Entropic Stigma Structure.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CosmicStigmaComponent : Component
{
    /// <summary>
    /// How long it takes for a cultist to destroy a Stigma.
    /// </summary>
    [DataField] public TimeSpan DestroyTime = TimeSpan.FromSeconds(20);

    /// <summary>
    /// Has this stigma been harvested?
    /// </summary>
    [DataField, AutoNetworkedField] public bool Harvested;

    /// <summary>
    /// Visual effect & sound effects.
    /// </summary>
    [DataField] public EntProtoId GenericVfx = "CosmicGenericVfx";
    [DataField] public SoundSpecifier HarvestSfx = new SoundPathSpecifier("/Audio/_ST/CosmicCult/stigma-harvest.ogg");
    [DataField] public SoundSpecifier DestroySfx = new SoundPathSpecifier("/Audio/_ST/CosmicCult/stigma-destroyed.ogg");
}
