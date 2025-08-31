using Robust.Shared.Prototypes;

namespace Content.Shared.Starlight.Economy;

[Prototype("SalaryRoleBonus")]
public sealed partial class SalaryRoleBonusPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public ulong[] Roles { get; set; } = [];

    [DataField(required: true)]
    public float Multiplayer;
}
