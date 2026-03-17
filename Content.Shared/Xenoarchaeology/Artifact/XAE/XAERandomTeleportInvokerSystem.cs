using Content.Shared.Popups;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAE.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Xenoarchaeology.Artifact.XAE;

public sealed class XAERandomTeleportInvokerSystem : BaseXAESystem<XAERandomTeleportInvokerComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedJointSystem _jointSystem = default!;

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAERandomTeleportInvokerComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;
        // todo: teleport person who activated artifact with artifact itself
        var component = ent.Comp;

        var xform = Transform(args.Artifact);
        _popup.PopupPredictedCoordinates(Loc.GetString("blink-artifact-popup"), xform.Coordinates, args.User, PopupType.Medium);

        var offsetTo = _random.NextVector2(component.MinRange, component.MaxRange);

        _xform.AttachToGridOrMap(args.Artifact);
        _jointSystem.ClearJoints(args.Artifact);
        _xform.SetCoordinates(args.Artifact, xform, xform.Coordinates.Offset(offsetTo));
    }
}
