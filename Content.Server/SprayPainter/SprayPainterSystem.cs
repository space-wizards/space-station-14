using Content.Server.Charges;
using Content.Server.Decals;
using Content.Server.Popups;
using Content.Shared.Charges.Components;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Database;
using Content.Shared.Decals;
using Content.Shared.Interaction;
using Content.Shared.SprayPainter;
using Content.Shared.SprayPainter.Components;
using Robust.Server.Audio;
using Robust.Server.GameObjects;

namespace Content.Server.SprayPainter;

/// <summary>
/// Handles spraying pipes and decals using a spray painter.
/// Other paintable objects are handled in shared.
/// </summary>
public sealed class SprayPainterSystem : SharedSprayPainterSystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly DecalSystem _decals = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ChargesSystem _charges = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SprayPainterComponent, AfterInteractEvent>(OnFloorAfterInteract);
    }

    /// <summary>
    /// Handles drawing decals when a spray painter is used to interact with the floor.
    /// Spray painter must have decal painting enabled and enough charges of paint to paint on the floor.
    /// </summary>
    private void OnFloorAfterInteract(Entity<SprayPainterComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target != null)
            return;

        // Includes both off and all other don't cares
        if (ent.Comp.DecalMode != DecalPaintMode.Add && ent.Comp.DecalMode != DecalPaintMode.Remove)
            return;

        args.Handled = true;
        if (TryComp(ent, out LimitedChargesComponent? charges) && _charges.GetCurrentCharges((ent, charges)) < ent.Comp.DecalChargeCost)
        {
            _popup.PopupEntity(Loc.GetString("spray-painter-interact-no-charges"), args.User, args.User);
            return;
        }

        var position = args.ClickLocation;
        if (ent.Comp.SnapDecals)
            position = position.SnapToGrid(EntityManager);

        if (ent.Comp.DecalMode == DecalPaintMode.Add)
        {
            // Offset painting for adding decals
            position = position.Offset(new(-0.5f));

            if (!_decals.TryAddDecal(ent.Comp.SelectedDecal, position, out _, ent.Comp.SelectedDecalColor, Angle.FromDegrees(ent.Comp.SelectedDecalAngle), 0, false))
                return;
        }
        else
        {
            var gridUid = _transform.GetGrid(args.ClickLocation);
            if (gridUid is not { } grid || !TryComp<DecalGridComponent>(grid, out var decalGridComp))
            {
                _popup.PopupEntity(Loc.GetString("spray-painter-interact-nothing-to-remove"), args.User, args.User);
                return;
            }

            var decals = _decals.GetDecalsInRange(grid, position.Position, validDelegate: IsDecalRemovable);
            if (decals.Count <= 0)
            {
                _popup.PopupEntity(Loc.GetString("spray-painter-interact-nothing-to-remove"), args.User, args.User);
                return;
            }

            foreach (var decal in decals)
            {
                _decals.RemoveDecal(grid, decal.Index, decalGridComp);
            }
        }

        _audio.PlayPvs(ent.Comp.SpraySound, ent);

        _charges.TryUseCharges((ent, charges), ent.Comp.DecalChargeCost);

        AdminLogger.Add(LogType.CrayonDraw, LogImpact.Low, $"{EntityManager.ToPrettyString(args.User):user} painted a {ent.Comp.SelectedDecal}");
    }

    /// <summary>
    /// Handles drawing decals when a spray painter is used to interact with the floor.
    /// Spray painter must have decal painting enabled and enough charges of paint to paint on the floor.
    /// </summary>
    private bool IsDecalRemovable(Decal decal)
    {
        if (!Proto.TryIndex<DecalPrototype>(decal.Id, out var decalProto))
            return false;

        return (decalProto.Tags.Contains("station")
            || decalProto.Tags.Contains("markings"))
            && !decalProto.Tags.Contains("dirty");
    }
}
