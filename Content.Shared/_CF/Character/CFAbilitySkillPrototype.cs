using Content.Shared.CF.Preferences;
using Robust.Shared.Prototypes;

namespace Content.Shared.CF.Character;

[Prototype("cfAbilitySkill")]
public sealed partial class CFAbilitySkillPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField(required: true)]
    public CFAbility Ability;

    [DataField]
    public int Order;
}
