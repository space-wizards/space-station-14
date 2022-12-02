using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

/// <summary>
/// This handles...
/// </summary>
public sealed class RandomTeleportArtifactSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<RandomTeleportArtifactComponent, ArtifactActivatedEvent>(OnActivate);
    }

    private void OnActivate(EntityUid uid, RandomTeleportArtifactComponent component, ArtifactActivatedEvent args)
    {
        var xform = Transform(uid);
        _popup.PopupCoordinates(Loc.GetString("blink-artifact-popup"), xform.Coordinates, PopupType.Medium);

        xform.Coordinates = xform.Coordinates.Offset(_random.NextVector2(component.Range));
    }
}
