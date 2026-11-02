using Content.Shared.CF.Preferences;
using Robust.Shared.Prototypes;

namespace Content.Shared.CF.Character;

[Prototype("cfBackground")]
public sealed partial class CFBackgroundPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField(required: true)]
    public LocId Description;

    [DataField]
    public List<ProtoId<CFAbilitySkillPrototype>> SkillBoosts = new();

    [DataField]
    public List<ProtoId<CFAbilitySkillPrototype>> TrainedSkills = new();

    [DataField]
    public List<CFAbilityBoost> AbilityBoosts = new();

    [DataField]
    public CFRarity Rarity = CFRarity.Common;
}
