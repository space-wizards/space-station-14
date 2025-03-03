using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;
using Content.Server.Chat.Systems;
namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Systems;

public sealed class ArtifacExpressionTriggerSystem : EntitySystem
{
    [Dependency] private readonly ArtifactSystem _artifact = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<TransformComponent, EntityEmotedEvent>(OnEmote);
    }

    private void OnEmote(EntityUid emoter, TransformComponent component, EntityEmotedEvent args)
    {
        var emoterXform = Transform(emoter);

        var toActivate = new List<Entity<ArtifactExpressionTriggerComponent>>();
        var query = EntityQueryEnumerator<ArtifactExpressionTriggerComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var trigger, out var xform))
        {
            if (!emoterXform.Coordinates.TryDistance(EntityManager, xform.Coordinates, out var distance))
                continue;

            if (distance > trigger.Range)
                continue;

            toActivate.Add((uid, trigger));
        }

        foreach (var a in toActivate)
        {
            _artifact.TryActivateArtifact(a);
        }
    }
}
