using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Enums;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Humanoid;

[RegisterComponent, NetworkedComponent]
public sealed class HumanoidComponent : Component
{
    /// <summary>
    ///     Current species. Dictates things like base body sprites,
    ///     base humanoid to spawn, etc.
    /// </summary>
    [DataField("species", customTypeSerializer: typeof(PrototypeIdSerializer<SpeciesPrototype>))]
    public string Species { get; set; } = string.Empty;

    /// <summary>
    ///     The initial profile and base layers to apply to this humanoid.
    /// </summary>
    [DataField("initial", customTypeSerializer: typeof(PrototypeIdSerializer<HumanoidProfilePrototype>))]
    public string? Initial { get; }

    /// <summary>
    ///     Skin color of this humanoid.
    /// </summary>
    [DataField("skinColor")]
    public Color SkinColor { get; set;  } = Color.FromHex("#C0967F");

    /// <summary>
    ///     Visual layers currently hidden. This will affect the base sprite
    ///     on this humanoid layer, and any markings that sit above it.
    /// </summary>
    [ViewVariables] public readonly HashSet<HumanoidVisualLayers> HiddenLayers = new();

    [DataField("sex")] public Sex Sex = Sex.Male;

    public MarkingSet CurrentMarkings = new();

    /// <summary>
    ///     Any custom base layers this humanoid might have. See:
    ///     limb transplants (potentially), robotic arms, etc.
    ///     Stored on the server, this is merged in the client into
    ///     all layer settings.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo> CustomBaseLayers = new();

    public HashSet<HumanoidVisualLayers> PermanentlyHidden = new();

    public HashSet<HumanoidVisualLayers> AllHiddenLayers
    {
        get
        {
            var result = new HashSet<HumanoidVisualLayers>(HiddenLayers);
            result.UnionWith(PermanentlyHidden);

            return result;
        }
    }

    // Couldn't these be somewhere else?
    [ViewVariables] public Gender Gender = default!;
    [ViewVariables] public int Age = 18;

    [ViewVariables] public List<Marking> CurrentClientMarkings = new();

    public Dictionary<HumanoidVisualLayers, HumanoidSpeciesSpriteLayer> BaseLayers = new();

    public string LastSpecies = default!;
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed class CustomBaseLayerInfo
{
    public CustomBaseLayerInfo(string id, Color color)
    {
        ID = id;
        Color = color;
    }

    /// <summary>
    ///     ID of this custom base layer. Must be a <see cref="HumanoidSpeciesSpriteLayer"/>.
    /// </summary>
    [DataField("id")]
    public string ID { get; }

    /// <summary>
    ///     Color of this custom base layer.
    /// </summary>
    [DataField("color")]
    public Color Color { get; }
}
