using System.Diagnostics.CodeAnalysis;
using Content.Shared.Humanoid.SkinColoration;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Humanoid.Prototypes;

/// <summary>
/// A prototype containing a SkinColorationStrategy
/// </summary>
[Prototype]
public sealed partial class SkinColorationPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The skin coloration strategy specified by this prototype
    /// </summary>
    [DataField(required: true)]
    public ISkinColorationStrategy Strategy = default!;

    //// <summary>
    ///     If true, will randomly generate realistic hair and eye colors.
    ///     Will also crush randomly generated colors down to the skin's luminosity
    ///     so markings don't appear too bright on darker skin.
    /// </summary>
    [DataField]
    public bool RealisticColors;

    /// <summary>
    ///     If true, will also squash hair and eye colors to the coloration strategy.
    /// </summary>
    [DataField]
    public bool SquashEyeHairColors;
}
