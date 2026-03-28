using Content.Shared.CCVar;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Atmos.Prototypes;

/// <summary>
/// Prototype defining a gas for atmospherics.
/// </summary>
/// <remarks>
/// The total number of gases is hardcoded in a bunch of places.
/// If you add any new ones, make sure to also adjust the constants in <see cref="Atmospherics"/> accordingly.
/// </remarks>
[Prototype]
public sealed partial class GasPrototype : IPrototype
{
    // TODO: Add interfaces for gas behaviours e.g. breathing, burning

    /// <inheritdoc />
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The name of the gas as shown to the player.
    /// </summary>
    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    /// The abbreviation of the name. For example O₂ for Oxygen.
    /// Used for UI purposes.
    /// </summary>
    [DataField(required: true)]
    public LocId Abbreviation;

    /// <summary>
    /// The molar heat capacity of this gas, in J/(K * mol).
    /// Describes how much heat energy is needed to heat up this gas by one Kelvin.
    /// Or in other words, the higher this number is the more energy this gas can store.
    /// </summary>
    /// <remarks>
    /// This will be divided by the <see cref="CCVars.AtmosHeatScale"/> cvar.
    /// </remarks>
    [DataField]
    public float MolarHeatCapacity;

    /// <summary>
    /// Heat capacity ratio for gas.
    /// TODO: Make gas pumps do proper adiabatic compression so that this is actually used.
    /// </summary>
    [DataField]
    public float HeatCapacityRatio = 1.4f;

    /// <summary>
    /// Molar mass of the gas.
    /// TODO: This is not used anywhere, do we even need this?
    /// </summary>
    [DataField]
    public float MolarMass = 1f;


    /// <summary>
    /// Minimum amount of moles for this gas to be visible.
    /// </summary>
    [DataField]
    public float GasMolesVisible = 0.25f;

    /// <summary>
    /// Visibility for this gas will be max after this value.
    /// </summary>
    [ViewVariables]
    public float GasMolesVisibleMax => GasMolesVisible * GasVisibilityFactor;

    /// <summary>
    /// Multiplier that decides when a gas will be at maximum visibility.
    /// </summary>
    [DataField]
    public float GasVisibilityFactor = Atmospherics.FactorGasVisibleMax;

    /// <summary>
    /// Sprite to show in the gas overlay if this gas is present on a tile.
    /// If null the gas will be invisible.
    /// </summary>
    [DataField]
    public SpriteSpecifier? GasOverlaySprite;

    /// <summary>
    /// The reagent that this gas will turn into when inhaled or condensed.
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype>? Reagent;

    /// <summary>
    /// The color of the gas used for UI purposes.
    /// </summary>
    [DataField]
    public Color Color = Color.White;

    /// <summary>
    /// The price per mole when this gas is sold at cargo.
    /// The final price will also depend on the purity of the gas mixture.
    /// </summary>
    [DataField]
    public float PricePerMole = 0;

    /// <summary>
    /// Whether the gas is considered to be flammable.
    /// This is used generically across Atmospherics to determine
    /// if things like hotspots are allowed to ignite if an
    /// oxidizer is present.
    /// </summary>
    [DataField]
    public bool IsFuel;

    /// <summary>
    /// Whether the gas is considered to be an oxidizer.
    /// Same reasoning as <see cref="IsFuel"/> but for oxidizers.
    /// </summary>
    [DataField]
    public bool IsOxidizer;
}
