using Content.Shared.Damage;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;
using Robust.Shared.Prototypes;

namespace Content.Shared.Nutrition;

[Prototype("satiationType")]
public sealed partial class SatiationTypePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;
}
