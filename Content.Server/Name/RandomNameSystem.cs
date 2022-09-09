using Content.Server.IdentityManagement;
using Content.Shared.Dataset;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Name;

/// <summary>
/// This handles generating random names from segments <see cref="RandomNameComponent"/>
/// </summary>
public sealed class RandomNameSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IdentitySystem _identity = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<RandomNameComponent, ComponentInit>((u,c,_) => GenerateName(u,c));
    }

    public void GenerateName(EntityUid uid, RandomNameComponent component)
    {
        if (component.Segments == null)
            return;

        var cleanedSegments = new List<string>();
        foreach (var segment in component.Segments)
        {
            //if it's a dataset, pick one of the words and use that
            if (_proto.TryIndex<DatasetPrototype>(segment, out var prototype))
            {
                cleanedSegments.Add(_random.Pick(prototype.Values));
            }
            else //otherwise, just use the raw segment itself
            {
                cleanedSegments.Add(segment);
            }
        }

        var name = string.Join(" ", cleanedSegments);
        MetaData(uid).EntityName = name;
        _identity.QueueIdentityUpdate(uid);
    }
}
