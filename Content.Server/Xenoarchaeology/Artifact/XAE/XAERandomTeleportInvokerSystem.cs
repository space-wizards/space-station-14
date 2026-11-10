using Content.Server.Xenoarchaeology.Artifact.XAE.Components;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Server.Xenoarchaeology.Artifact.XAE;

/// <summary>
/// System for teleporting artifact activator into random position.
/// </summary>
public sealed partial class XAERandomTeleportInvokerSystem : BaseXAESystem<XAERandomTeleportInvokerComponent>
{
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedJointSystem _jointSystem = default!;

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAERandomTeleportInvokerComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));

        // todo: for prediction we need to have delay between activation and make a viewersub at a target spot
        // todo: teleport person who activated artifact with artifact itself
        var component = ent.Comp;

        var xform = Transform(args.Artifact);
        _popup.PopupCoordinates(Loc.GetString("blink-artifact-popup"), xform.Coordinates, PopupType.Medium);

        var offsetTo = random.NextVector2(component.MinRange, component.MaxRange);

        _xform.AttachToGridOrMap(args.Artifact);
        _jointSystem.ClearJoints(args.Artifact);
        _xform.SetCoordinates(args.Artifact, xform, xform.Coordinates.Offset(offsetTo));
    }
}
