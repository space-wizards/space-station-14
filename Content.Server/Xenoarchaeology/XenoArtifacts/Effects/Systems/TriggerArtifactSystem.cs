using Content.Server.Explosion.EntitySystems;
using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

/// <summary>
/// This handles <see cref="TriggerArtifactComponent"/>
/// </summary>
public sealed class TriggerArtifactSystem : EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<TriggerArtifactComponent, ArtifactActivatedEvent>(OnArtifactActivated);
    }

    private void OnArtifactActivated(EntityUid uid, TriggerArtifactComponent component, ArtifactActivatedEvent args)
    {
        _trigger.Trigger(uid, args.Activator);
    }
}
