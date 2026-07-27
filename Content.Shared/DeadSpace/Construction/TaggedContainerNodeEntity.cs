// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Construction;
using Content.Shared.Tag;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeadSpace.Construction;

/// <summary>
/// Selects a construction node entity when the first entity in a container has a configured tag.
/// </summary>
[UsedImplicitly]
[DataDefinition]
public sealed partial class TaggedContainerNodeEntity : IGraphNodeEntity
{
    [DataField(required: true)]
    public string Container { get; private set; } = string.Empty;

    [DataField(required: true)]
    public ProtoId<TagPrototype> Tag { get; private set; }

    [DataField(required: true)]
    public EntProtoId Tagged { get; private set; }

    [DataField(required: true)]
    public EntProtoId Fallback { get; private set; }

    public string GetId(EntityUid? uid, EntityUid? userUid, GraphNodeEntityArgs args)
    {
        if (uid == null)
            return Fallback;

        var containerSystem = args.EntityManager.EntitySysManager.GetEntitySystem<SharedContainerSystem>();
        var tagSystem = args.EntityManager.EntitySysManager.GetEntitySystem<TagSystem>();

        if (!containerSystem.TryGetContainer(uid.Value, Container, out var container) ||
            container.ContainedEntities.Count == 0)
        {
            return Fallback;
        }

        return tagSystem.HasTag(container.ContainedEntities[0], Tag)
            ? Tagged
            : Fallback;
    }
}
