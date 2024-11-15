using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.Starlight.Economy;
public abstract partial class SharedSalarySystem : EntitySystem
{
}
[Prototype("Salaries")]
public sealed partial class SalariesPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public Dictionary<ProtoId<JobPrototype>, int> Jobs = new();

    [DataField]
    public Dictionary<ProtoId<AntagPrototype>, int> Antags = new();
}
