using Content.Shared.Dataset;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Humanoid.Prototypes;

[Prototype]
public sealed partial class SpeciesPrototype : IPrototype
{
    /// <summary>
    /// Prototype ID of the species.
    /// </summary>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// User visible name of the species.
    /// </summary>
    [DataField(required: true)]
    public string Name { get; private set; } = default!;

    /// <summary>
    ///     Descriptor. Unused...? This is intended
    ///     for an eventual integration into IdentitySystem
    ///     (i.e., young human person, young lizard person, etc.)
    /// </summary>
    [DataField]
    public string Descriptor { get; private set; } = "humanoid";

    /// <summary>
    /// Whether the species is available "at round start" (In the character editor)
    /// </summary>
    [DataField(required: true)]
    public bool RoundStart { get; private set; } = false;

    // The below two are to avoid fetching information about the species from the entity
    // prototype.

    // This one here is a utility field, and is meant to *avoid* having to duplicate
    // the massive SpriteComponent found in every species.
    // Species implementors can just override SpriteComponent if they want a custom
    // sprite layout, and leave this null. Keep in mind that this will disable
    // sprite accessories.

    [DataField("sprites")]
    public ProtoId<HumanoidSpeciesBaseSpritesPrototype> SpriteSet { get; private set; } = default!;

    /// <summary>
    ///     Default skin tone for this species. This applies for non-human skin tones.
    /// </summary>
    [DataField]
    public Color DefaultSkinTone { get; private set; } = Color.White;

    /// <summary>
    ///     Default human skin tone for this species. This applies for human skin tones.
    ///     See <see cref="SkinColor.HumanSkinTone"/> for the valid range of skin tones.
    /// </summary>
    [DataField]
    public int DefaultHumanSkinTone { get; private set; } = 20;

    /// <summary>
    ///     The limit of body markings that you can place on this species.
    /// </summary>
    [DataField("markingLimits")]
    public ProtoId<MarkingPointsPrototype> MarkingPoints { get; private set; } = default!;

    /// <summary>
    ///     Humanoid species variant used by this entity.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Prototype { get; private set; } = default!;

    /// <summary>
    /// Prototype used by the species for the dress-up doll in various menus.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId DollPrototype { get; private set; } = default!;

    /// <summary>
    /// Starlight
    /// Allow Custom Specie Name for this Specie.
    /// </summary>
    [DataField]
    public Boolean CustomName { get; private set; } = false;

    /// <summary>
    /// Method of skin coloration used by the species.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<SkinColorationPrototype> SkinColoration { get; private set; }

    /// <summary>
    /// Starlight
    /// Method of eyes coloration used by the species.
    /// </summary>
    [DataField]
    public HumanoidEyeColor EyeColoration { get; private set; } = HumanoidEyeColor.Standard;

    [DataField]
    public ProtoId<LocalizedDatasetPrototype> MaleFirstNames { get; private set; } = "NamesFirstMale";

    [DataField]
    public ProtoId<LocalizedDatasetPrototype> FemaleFirstNames { get; private set; } = "NamesFirstFemale";

    [DataField]
    public ProtoId<LocalizedDatasetPrototype> LastNames { get; private set; } = "NamesLast";

    [DataField]
    public SpeciesNaming Naming { get; private set; } = SpeciesNaming.FirstLast;

    [DataField]
    public List<Sex> Sexes { get; private set; } = new() { Sex.Male, Sex.Female };

    /// <summary>
    ///     Characters younger than this are too young to be hired by Nanotrasen.
    /// </summary>
    [DataField]
    public int MinAge = 18;

    /// <summary>
    ///     Characters younger than this appear young.
    /// </summary>
    [DataField]
    public int YoungAge = 30;

    /// <summary>
    ///     Characters older than this appear old. Characters in between young and old age appear middle aged.
    /// </summary>
    [DataField]
    public int OldAge = 60;

    /// <summary>
    ///     Characters cannot be older than this. Only used for restrictions...
    ///     although imagine if ghosts could age people WYCI...
    /// </summary>
    [DataField]
    public int MaxAge = 120;

    //starlight start

    /// <summary>
    ///     Characters must not crumple under earth-like gravity.
    /// </summary>
    [DataField]
    public float MinWidth = 0.9f;

    /// <summary>
    ///     Characters must not exhibit a measurable gravitational pull on nearby objects.
    /// </summary>
    [DataField]
    public float MaxWidth = 1.1f;

    /// <summary>
    ///     The normal width for this species.
    /// </summary>
    [DataField]
    public float DefaultWidth = 1f;

    /// <summary>
    ///     Sentient microbial lifeforms are not currently hireable under contract.
    /// </summary>
    [DataField]
    public float MinHeight = 0.9f;

    /// <summary>
    ///     You cannot fit in our cloning pods.
    /// </summary>
    [DataField]
    public float MaxHeight = 1.15f;

    /// <summary>
    ///     The normal height for this species.
    /// </summary>
    [DataField]
    public float DefaultHeight = 1f;

    /// <summary>
    ///     The height of this species in CM if it were 1x tall
    /// </summary>
    [DataField]
    public int StandardSize = 170;

    /// <summary>
    ///     The weight of this species in KG if it were 1x tall and 1x wide
    /// </summary>
    [DataField]
    public int StandardWeight = 70;

    /// <summary>
    ///     How much this species' weight increases or decreases depending on unit size, measured in KG/units^2
    /// </summary>
    [DataField]
    public int StandardDensity = 110;
    //starlight end

    /// Starlight
    /// <summary>
    ///     How many points species get for installing cybernetics at roundstart
    ///     Can be used to disable roundstart cybernetics
    /// </summary>
    [DataField]
    public int RoundstartCyberwareCapacity = 3;

}

public enum SpeciesNaming : byte
{
    First,
    FirstLast,
    FirstDashFirst,
    TheFirstofLast,
}
