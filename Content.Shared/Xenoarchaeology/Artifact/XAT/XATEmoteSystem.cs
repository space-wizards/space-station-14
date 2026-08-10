using Content.Shared.Chat;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT;

/// <summary>
/// System for xeno artifact trigger that requires a specific emote from any mob near artifact.
/// </summary>
public sealed partial class XATEmoteSystem : BaseXATSystem<XATEmoteComponent>
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private EntityQuery<XenoArtifactComponent> _xenoArtifactQuery;

    [SubscribeLocalEvent]
    private void OnEmote(ref BeforeEmoteEvent args)
    {
        if (args.Cancelled)
            return;

        // get the coordinates of our emoter.
        var targetCoords = Transform(args.Source).Coordinates;

        // iterate over all artifacts and see if we can trigger any of them with this emote.
        var query = EntityQueryEnumerator<XATEmoteComponent, XenoArtifactNodeComponent>();
        while (query.MoveNext(out var uid, out var comp, out var node))
        {
            if (node.Attached == null)
                continue;

            if (!comp.Emotes.Contains(args.Emote))
                continue;

            var artifact = _xenoArtifactQuery.Get(node.Attached.Value);

            if (!CanTrigger(artifact, (uid, node)))
                continue;

            var artifactCoords = Transform(artifact).Coordinates;
            if (_transform.InRange(targetCoords, artifactCoords, comp.Range)) 
                Trigger(artifact, (uid, comp, node));
        }
    }
}
