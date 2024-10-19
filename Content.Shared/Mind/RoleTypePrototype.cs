using Robust.Shared.Prototypes;

namespace Content.Shared.Mind;

/// <summary>
///     The core properties of Role Types, not intended to be alterable or uploadable under any conditions
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
    public string Name = "role-type-neutral-name";

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
    SubvertedSilicon, //TODO:ERRANT ask admins
    Silicon,
    TeamAntagonist,
    SoloAntagonist,
    FreeAgent,
    Familiar,
    Neutral
}
