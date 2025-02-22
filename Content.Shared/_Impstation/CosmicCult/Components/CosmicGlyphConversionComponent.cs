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
            ["Brute"] = -50,
            ["Burn"] = -50
        }
    };
}
