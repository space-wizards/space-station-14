using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Xenoarchaeology.Artifact.XAE.Components;
using Robust.Shared.Timing;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Xenoarchaeology.Artifact.XAE;

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
