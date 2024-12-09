using Robust.Shared.Prototypes;

namespace Content.Shared.Mind;

/// <summary>
///     The core properties of Role Types
/// </summary>
[Prototype, Serializable]
public sealed class RoleTypePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     The role's specific antag-or-other-special category.
    /// </summary>
    [DataField(required: true)]
    public RoleEnum RoleRule = RoleEnum.Neutral;

    /// <summary>
    ///     The role's name as displayed on the UI.
    /// </summary>
    [DataField(required: true)]
    public string Name = "role-type-crew-aligned-name";

    /// <summary>
    ///     The role's displayed color.
    /// </summary>
    [DataField]
    public Color Color { get; private set; } = Color.FromHex("#eeeeee");
}

/// <summary>
///     The possible roles a character can be in the round.
/// </summary>
public enum RoleEnum
{
    SiliconAntagonist,
    Silicon,
    TeamAntagonist,
    SoloAntagonist,
    FreeAgent,
    Familiar,
    Neutral
}
