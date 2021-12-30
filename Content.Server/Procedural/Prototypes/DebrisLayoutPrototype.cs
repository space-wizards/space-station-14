using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Procedural.Prototypes;

[Prototype("debrisLayout")]
public class DebrisLayoutPrototype : IPrototype
{
    [DataField("id")]
    public string ID { get; } = default!;

    [DataField("layout")]
    public List<DebrisLayoutEntry> Layout { get; } = default!;

    public DebrisPrototype? Pick()
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
        foreach (var entry in Layout)
        {
            if (random.Prob(entry.Probability))
            {
                // This will always succeed due to the custom serializer, thus no check.
                return prototypeManager.Index<DebrisPrototype>(entry.DebrisPrototype);
            }
        }

        return null;
    }
}

[DataDefinition]
public struct DebrisLayoutEntry
{
    [DataField("proto", customTypeSerializer: typeof(PrototypeIdSerializer<DebrisPrototype>))]
    public string DebrisPrototype = default!;

    [DataField("prob")]
    public float Probability;
}
