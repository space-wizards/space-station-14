using System.Collections.Frozen;
using Content.Shared.Body.Prototypes;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Nutrition;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.Components.Reagents;

[RegisterComponent, NetworkedComponent]
public sealed partial class ReagentDefinitionComponent : Component
{
    [DataField(required: true)]
    public string Id = string.Empty;

    [DataField("Name",required: true)]
    public LocId NameLocId { get; set; }

    [ViewVariables(VVAccess.ReadOnly)]
    public string LocalizedName => Loc.GetString(NameLocId);

    [DataField]
    public string Group { get; set; } = "Unknown";

    /// <summary>
    ///     Is this reagent recognizable to the average spaceman (water, welding fuel, ketchup, etc)?
    /// </summary>
    [DataField]
    public bool Recognizable;

    [DataField]
    public float PricePerUnit;

    [DataField]
    public FixedPoint4 MolarMass = 18;

    /// <summary>
    /// Gas constant for thermal expansion, in moles/kelvin
    /// Observed value for water.
    /// </summary>
    [DataField]
    public float ExpansionConstant = 8.314f;

    [DataField]
    public ProtoId<FlavorPrototype>? Flavor;

    [DataField("desc", required: true)]
    public LocId DescriptionLocId { get; set; }

    [ViewVariables(VVAccess.ReadOnly)]
    public string LocalizedDescription => Loc.GetString(DescriptionLocId);

    [DataField("physicalDesc", required: true)]
    public LocId PhysicalDescriptionLocId { get; set; } = default!;

    [ViewVariables(VVAccess.ReadOnly)]
    public string LocalizedPhysicalDescription => Loc.GetString(PhysicalDescriptionLocId);

    /// <summary>
    /// There must be at least this much quantity in a solution to be tasted.
    /// </summary>
    [DataField]
    public FixedPoint4 FlavorMinimum = FixedPoint4.New(0.1f);

    [DataField("color")]
    public Color SubstanceColor { get; set; } = Color.White;

    /// <summary>
    ///     The specific heat of the reagent.
    ///     How much energy it takes to heat one unit of this reagent by one Kelvin.
    /// </summary>
    [DataField]
    public float SpecificHeat { get; set; } = 1.0f;

    [DataField]
    public float? BoilingPoint { get; set; }

    [DataField]
    public float? MeltingPoint { get; set; }

    /// <summary>
    /// If this reagent is part of a puddle is it slippery.
    /// </summary>
    [DataField]
    public bool Slippery;

    /// <summary>
    /// How easily this reagent becomes fizzy when aggitated.
    /// 0 - completely flat, 1 - fizzes up when nudged.
    /// </summary>
    [DataField]
    public float Fizziness;

    /// <summary>
    /// How much reagent slows entities down if it's part of a puddle.
    /// 0 - no slowdown; 1 - can't move.
    /// </summary>
    [DataField]
    public float Viscosity;

    [DataField]
    public SoundSpecifier FootstepSound = new SoundCollectionSpecifier("FootstepWater", AudioParams.Default.WithVolume(6));

    /// <summary>
    /// Should this reagent work on the dead?
    /// </summary>
    [DataField]
    public bool WorksOnTheDead;

    [DataField(serverOnly: true)]
    public FrozenDictionary<ProtoId<MetabolismGroupPrototype>, ReagentEffectsEntry>? Metabolisms;

    [DataField(serverOnly: true)]
    public Dictionary<ProtoId<ReactiveGroupPrototype>, Reagent.ReactiveReagentEffectEntry>? ReactiveEffects;

    [DataField(serverOnly: true)]
    public List<ITileReaction> TileReactions = new(0);

    [DataField("plantMetabolism", serverOnly: true)]
    public List<EntityEffect> PlantMetabolisms = new(0);
}
