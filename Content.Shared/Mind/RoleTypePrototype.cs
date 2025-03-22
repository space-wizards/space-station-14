using Robust.Shared.Prototypes;

namespace Content.Shared.Mind;

/// <summary>
///     The core properties of Role Types
/// </summary>
[Prototype, Serializable]
public sealed partial class RoleTypePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     The role's name as displayed on the UI.
    /// </summary>
    [DataField]
    public LocId Name = "role-type-crew-aligned-name";

    /// <summary>
    ///     The role's displayed color.
    /// </summary>
    [DataField]
    public Color Color { get; private set; } = Color.FromHex("#eeeeee");

    /// <summary>
    ///     A symbol used to represent the role type.
    /// </summary>
    [DataField]
    public string Symbol = string.Empty;
}
