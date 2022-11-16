using Robust.Shared.Prototypes;
using Content.Shared.Damage;
namespace Content.Shared.Body.Prototypes;

[Prototype("bodyCovering")]
public sealed class BodyCoveringPrototype : IPrototype
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
    public DamageModifierSet Resistance = new();

    [DataField("hardened", required: false)]
    public bool Hardened = false;
}
