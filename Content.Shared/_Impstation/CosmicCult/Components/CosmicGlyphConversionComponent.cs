using Content.Shared.Damage;

namespace Content.Shared._Impstation.CosmicCult.Components;

[RegisterComponent]
public sealed partial class CosmicGlyphConversionComponent : Component
{
    /// <summary>
    ///     The search range for finding conversion targets.
    /// </summary>
    [DataField]
    public float ConversionRange = 0.5f;

    /// <summary>
    ///     Whether or not we ignore mindshields.
    /// </summary>
    [DataField]
    public bool NegateProtection = false;

    /// <summary>
    ///     Healing applied on conversion.
    /// </summary>
    [DataField]
    public DamageSpecifier ConversionHeal = new()
    {
        DamageDict = new()
        {
            { "Blunt", 50},
            { "Slash", 50},
            { "Piercing", 50},
            { "Heat", 50},
            { "Shock", 50},
            { "Cold", 50},
            { "Poison", 50},
            { "Radiation", 50},
            { "Asphyxiation", 50}
        }
    };
}
