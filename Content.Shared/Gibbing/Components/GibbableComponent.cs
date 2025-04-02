using Content.Shared.Gibbing.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Gibbing.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(GibbingSystem))]
public sealed partial class GibbableComponent : Component
{
    /// <summary>
    /// Giblet entity prototypes to randomly select from when spawning additional giblets
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntProtoId> GibPrototypes = new();

    /// <summary>
    /// Number of giblet entities to spawn in addition to entity contents
    /// </summary>
    [DataField, AutoNetworkedField]
    public int GibCount;

    /// <summary>
    /// Sound to be played when this entity is gibbed, only played when playsound is true on the gibbing function
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? GibSound = new SoundCollectionSpecifier("gib", AudioParams.Default.WithVariation(0.025f));

    /// <summary>
    /// Max distance giblets can be dropped from an entity when NOT using physics-based scattering
    /// </summary>
    [DataField, AutoNetworkedField]
    public float GibScatterRange = 0.3f;
}
