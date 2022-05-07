using Content.Shared.Dataset;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.RandomMetadata;

public sealed class RandomMetadataSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomMetadataComponent, MapInitEvent>(OnMapInit);
    }

    // This is done on map init so that map-placed entities have it randomized each time the map loads, for fun.
    private void OnMapInit(EntityUid uid, RandomMetadataComponent component, MapInitEvent args)
    {
        var meta = MetaData(uid);

        if (component.NameSet != null)
        {
            var nameProto = _prototype.Index<DatasetPrototype>(component.NameSet);
            meta.EntityName = _random.Pick(nameProto.Values);
        }

        if (component.DescriptionSet != null)
        {
            var descProto = _prototype.Index<DatasetPrototype>(component.DescriptionSet);
            meta.EntityDescription = _random.Pick(descProto.Values);
        }
    }
}
