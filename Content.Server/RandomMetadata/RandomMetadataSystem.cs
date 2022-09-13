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

        if (component.NameSegments != null)
        {
            var outputSegments = new List<string>();
            foreach (var segment in component.NameSegments)
            {
                outputSegments.Add(_prototype.TryIndex<DatasetPrototype>(segment, out var proto)
                    ? _random.Pick(proto.Values)
                    : segment);
            }
            meta.EntityName = string.Join(component.NameSeparator, outputSegments);
        }

        if (component.DescriptionSegments != null)
        {
            var outputSegments = new List<string>();
            foreach (var segment in component.DescriptionSegments)
            {
                outputSegments.Add(_prototype.TryIndex<DatasetPrototype>(segment, out var proto)
                    ? _random.Pick(proto.Values)
                    : segment);
            }
            meta.EntityDescription = string.Join(component.DescriptionSeparator, outputSegments);
        }
    }
}
