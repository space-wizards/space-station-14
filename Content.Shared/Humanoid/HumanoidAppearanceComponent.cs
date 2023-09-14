using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Enums;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;
using static Content.Shared.Humanoid.HumanoidAppearanceState;

namespace Content.Shared.Humanoid;

[NetworkedComponent, RegisterComponent]
public sealed partial class HumanoidAppearanceComponent : Component
{
    [DataField("markingSet")]
    public MarkingSet MarkingSet = new();

    [DataField("baseLayers")]
    public Dictionary<HumanoidVisualLayers, HumanoidSpeciesSpriteLayer> BaseLayers = new();

    [DataField("permanentlyHidden")]
    public HashSet<HumanoidVisualLayers> PermanentlyHidden = new();

    // Couldn't these be somewhere else?

    [DataField("gender")]
    [ViewVariables] public Gender Gender = default!;

    [DataField("age")]
    [ViewVariables] public int Age = 18;

    /// <summary>
    ///     Any custom base layers this humanoid might have. See:
    ///     limb transplants (potentially), robotic arms, etc.
    ///     Stored on the server, this is merged in the client into
    ///     all layer settings.
    /// </summary>
    [DataField("customBaseLayers")]
    public Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo> CustomBaseLayers = new();

    /// <summary>
    ///     Current species. Dictates things like base body sprites,
    ///     base humanoid to spawn, etc.
    /// </summary>
    [DataField("species", customTypeSerializer: typeof(PrototypeIdSerializer<SpeciesPrototype>), required: true)]
    public string Species { get; set; } = default!;

    /// <summary>
    ///     The initial profile and base layers to apply to this humanoid.
    /// </summary>
    [DataField("initial", customTypeSerializer: typeof(PrototypeIdSerializer<HumanoidProfilePrototype>))]
    public string? Initial { get; private set; }

    /// <summary>
    ///     Skin color of this humanoid.
    /// </summary>
    [DataField("skinColor")]
    public Color SkinColor { get; set; } = Color.FromHex("#C0967F");

    /// <summary>
    ///     Visual layers currently hidden. This will affect the base sprite
    ///     on this humanoid layer, and any markings that sit above it.
    /// </summary>
    [DataField("hiddenLayers")]
    public HashSet<HumanoidVisualLayers> HiddenLayers = new();

    [DataField("sex")]
    public Sex Sex = Sex.Male;

    [DataField("eyeColor")]
    public Color EyeColor = Color.Brown;

    /// <summary>
    ///     Hair color of this humanoid. Used to avoid looping through all markings
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Color? CachedHairColor;

    /// <summary>
    ///     Facial Hair color of this humanoid. Used to avoid looping through all markings
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Color? CachedFacialHairColor;
}

[Serializable, NetSerializable]
public sealed partial class HumanoidAppearanceState : ComponentState
{
    public readonly MarkingSet Markings;
    public readonly HashSet<HumanoidVisualLayers> PermanentlyHidden;
    public readonly HashSet<HumanoidVisualLayers> HiddenLayers;
    public readonly Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo> CustomBaseLayers;
    public readonly Sex Sex;
    public readonly Gender Gender;
    public readonly int Age = 18;
    public readonly string Species;
    public readonly Color SkinColor;
    public readonly Color EyeColor;

    public HumanoidAppearanceState(
        MarkingSet currentMarkings,
        HashSet<HumanoidVisualLayers> permanentlyHidden,
        HashSet<HumanoidVisualLayers> hiddenLayers,
        Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo> customBaseLayers,
        Sex sex,
        Gender gender,
        int age,
        string species,
        Color skinColor,
        Color eyeColor)
    {
        Markings = currentMarkings;
        PermanentlyHidden = permanentlyHidden;
        HiddenLayers = hiddenLayers;
        CustomBaseLayers = customBaseLayers;
        Sex = sex;
        Gender = gender;
        Age = age;
        Species = species;
        SkinColor = skinColor;
        EyeColor = eyeColor;
    }

    [DataDefinition]
    [Serializable, NetSerializable]
    public readonly partial struct CustomBaseLayerInfo
    {
        public CustomBaseLayerInfo(string? id, Color? color = null)
        {
            DebugTools.Assert(id == null || IoCManager.Resolve<IPrototypeManager>().HasIndex<HumanoidSpeciesSpriteLayer>(id));
            ID = id;
            Color = color;
        }

        /// <summary>
        ///     ID of this custom base layer. Must be a <see cref="HumanoidSpeciesSpriteLayer"/>.
        /// </summary>
        [DataField("id", customTypeSerializer: typeof(PrototypeIdSerializer<HumanoidSpeciesSpriteLayer>))]
        public string? ID { init; get; }

        /// <summary>
        ///     Color of this custom base layer. Null implies skin colour if the corresponding <see cref="HumanoidSpeciesSpriteLayer"/> is set to match skin.
        /// </summary>
        [DataField("color")]
        public Color? Color { init; get; }
    }
}
