using Content.Server.Procedural;
using Content.Server.Worldgen.Prototypes;
using Content.Server.Worldgen.Systems;
using Content.Shared.Maps;
using Content.Shared.Procedural;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._FTL.FTLPoints.Effects;

[DataDefinition]
public sealed class ApplyWorldGenConfigEffect : FTLPointEffect
{
    [DataField("config", customTypeSerializer:typeof(PrototypeIdSerializer<WorldgenConfigPrototype>))]
    public string ConfigPrototype = "Default";

    public override void Effect(FTLPointEffectArgs args)
    {
        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
        var ser = IoCManager.Resolve<ISerializationManager>();

        if (!prototypeManager.TryIndex<WorldgenConfigPrototype>(ConfigPrototype, out var proto))
        {
            return;
        }

        proto.Apply(args.MapUid, ser, args.EntityManager);
    }
}
