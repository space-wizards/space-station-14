using Robust.Shared.Prototypes;

namespace Content.Shared.Objectives.Prototypes;

/// <summary>
/// Prototype for objective issuers.
/// They represent organizations that issue objectives, used for grouping and as a header above common objectives.
/// </summary>
[Prototype]
public sealed partial class ObjectiveIssuerPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The LocId of the issuer name.
    /// </summary>
    [DataField(required: true)]
    private LocId Name { get; set; }

    /// <summary>
    /// Localized version of the issuer name.
    /// </summary>
    [ViewVariables]
    public string LocalizedName => Loc.GetString(Name);
}
