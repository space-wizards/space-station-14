using Content.Server.Worldgen.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Maps;

public sealed partial class GameMapPrototype
{
    [DataField("worldgenConfig", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<WorldgenConfigPrototype>))]
    public string WorldgenConfig = default!;
}
