using Content.Shared.Dataset;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.RandomMetadata;

public sealed class RandomMetadataSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

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
            _metaData.SetEntityName(uid, GetRandomFromSegments(component.NameSegments, component.NameSeparator), meta);
        }

        if (component.DescriptionSegments != null)
        {
            _metaData.SetEntityDescription(uid,
                GetRandomFromSegments(component.DescriptionSegments, component.DescriptionSeparator), meta);
        }
    }

    /// <summary>
    /// Generates a random string from segments and a separator.
    /// </summary>
    /// <param name="segments">The segments that it will be generated from</param>
    /// <param name="separator">The separator that will be inbetween each segment</param>
    /// <returns>The newly generated string</returns>
    [PublicAPI]
    public string GetRandomFromSegments(List<string> segments, string? separator)
    {
        var outputSegments = new List<string>();
        foreach (var segment in segments)
        {
            if (_prototype.TryIndex<DatasetPrototype>(segment, out var proto)) {
                var random = _random.Pick(proto.Values);
                if (Loc.TryGetString(random, out var localizedSegment))
                    outputSegments.Add(localizedSegment);
                else
                    outputSegments.Add(random);
            } else if (Loc.TryGetString(segment, out var localizedSegment))
                outputSegments.Add(localizedSegment);
            else
                outputSegments.Add(segment);
        }
        return string.Join(separator, outputSegments);
    }
}
