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
    [Dependency] private EntityQuery<XenoArtifactComponent> _xenoArtifactQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BeforeEmoteEvent>(OnEmote); //Directly listens for all emote attempts.
    }

    private void OnEmote(ref BeforeEmoteEvent args)
    {
        if (args.Cancelled == true)
            return;

        var targetCoords = Transform(args.Source).Coordinates; // get the coordinates of our emoter.

        var query = EntityQueryEnumerator<XATEmoteComponent, XenoArtifactNodeComponent>(); // Find all artifact nodes with this component.
        while (query.MoveNext(out var uid, out var comp, out var node))  // For each node with this trigger component.
        {
            if (node.Attached == null) // Is it part of an artifact.
                continue;

            if (!comp.Emotes.Contains(args.Emote)) // Does the emote match our list.
                continue;

            var artifact = _xenoArtifactQuery.Get(node.Attached.Value); // Get the artifact this node is a part of.

            if (!CanTrigger(artifact, (uid, node))) // Can this node currently trigger.
                continue;

            var artifactCoords = Transform(artifact).Coordinates;
            if (_transform.InRange(targetCoords, artifactCoords, comp.Range)) // Are we within range.
                Trigger(artifact, (uid, comp, node));
        }
    }
}
