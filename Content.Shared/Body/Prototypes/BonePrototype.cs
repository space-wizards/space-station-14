using Content.Shared.Damage;
using Robust.Shared.Prototypes;

namespace Content.Shared.Body.Prototypes;

[Prototype("bone")]
public sealed class BonePrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    private string _name = string.Empty;

    [DataField("name")]
    public string Name
    {
        get => _name;
        private set => _name = Loc.GetString(value);
    }

    [DataField("description", required: false)]
    public string Description = string.Empty;

    [DataField("damageResistance", required: false)]
    public DamageSpecifier Resistance = new DamageSpecifier();

    //support for exoskeletons because bugs and xenos are cool. Also crabs. Because everything returns to crab.
    [DataField("internal", required: false)]
    public bool Internal = true;

    [DataField("structural", required: false)]
    public bool Structure = true;

    //this gets overidden if internal is false, because exoskeletons block access to organs
    [DataField("blocksOrgans", required: false)]
    public bool BlocksOrgans = false;
}
