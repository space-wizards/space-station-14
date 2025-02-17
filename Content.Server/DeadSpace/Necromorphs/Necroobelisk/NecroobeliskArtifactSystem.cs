// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.Explosion.EntitySystems;
using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.DeadSpace.Necromorphs.Necroobelisk;

namespace Content.Server.DeadSpace.Necromorphs.Necroobelisk;

/// <summary>
/// This handles <see cref="TriggerArtifactComponent"/>
/// </summary>
public sealed class NecroobeliskArtifactSystem : EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;
    [Dependency] private readonly NecroobeliskSystem _necroobelisk = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<TriggerArtifactComponent, ArtifactNodeEnteredEvent>(OnArtifactNodeEntered);
    }

    private void OnArtifactNodeEntered(EntityUid uid, TriggerArtifactComponent component, ArtifactNodeEnteredEvent args)
    {
        if (TryComp<NecroobeliskComponent>(uid, out var necroobeliskComp))
            _necroobelisk.ToggleObeliskActive(uid, necroobeliskComp);
    }
}
