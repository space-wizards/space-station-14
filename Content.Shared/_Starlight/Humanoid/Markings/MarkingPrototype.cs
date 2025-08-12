using System.Numerics;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

// ReSharper disable CheckNamespace

namespace Content.Shared.Humanoid.Markings;

public sealed partial class MarkingPrototype : IInheritingPrototype
{
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<MarkingPrototype>))]
    public string[]? Parents { get; }

    [AbstractDataField]
    [NeverPushInheritance]
    public bool Abstract { get; }

    [DataField]
    public string? WaggingId;
}
