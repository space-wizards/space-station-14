using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Client.Construction;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Placement;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Placement;

/// <summary>
/// Draws a directional arrow during entity placement preview for any entity prototype
/// that has a <see cref="PlacementDirectionIndicatorComponent"/>.
/// </summary>
public sealed partial class PlacementDirectionIndicatorOverlay : Overlay
{
    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private IPlacementManager _placement = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    private readonly SpriteSystem _sprite;
    private readonly SharedTransformSystem _xform;
    private readonly ConstructionSystem _construction;

    public PlacementDirectionIndicatorOverlay()
    {
        IoCManager.InjectDependencies(this);

        _sprite = _entMan.System<SpriteSystem>();
        _xform = _entMan.System<SharedTransformSystem>();
        _construction = _entMan.System<ConstructionSystem>();
    }

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_placement.IsActive || _placement.Eraser)
            return;

        if (_placement.CurrentMode is not { MouseCoords: var mouseCoords } || !mouseCoords.EntityId.IsValid())
            return;

        var mapCoords = _xform.ToMapCoordinates(mouseCoords);
        if (mapCoords.MapId != args.MapId)
            return;

        if (!TryGetPlacingPrototype(out var placingProto))
            return;

        if (!placingProto.TryComp<PlacementDirectionIndicatorComponent>(out var indicator, _entMan.ComponentFactory))
            return;

        var gridRotation = _entMan.HasComponent<MapGridComponent>(mouseCoords.EntityId)
            ? _xform.GetWorldRotation(mouseCoords.EntityId)
            : Angle.Zero;

        var worldDirAngle = _placement.Direction.ToAngle() + gridRotation;
        var textureAngle = worldDirAngle - indicator.SpriteNaturalAngle;

        var texture = _sprite.Frame0(indicator.Sprite);
        var worldPos = mapCoords.Position + worldDirAngle.RotateVec(indicator.Offset);
        var box = Box2.CenteredAround(worldPos, new Vector2(texture.Width, texture.Height) / EyeManager.PixelsPerMeter);

        args.WorldHandle.SetTransform(Matrix3x2.Identity);
        args.WorldHandle.DrawTextureRect(texture, new Box2Rotated(box, textureAngle, worldPos), indicator.Color);
    }

    private bool TryGetPlacingPrototype([NotNullWhen(true)] out EntityPrototype? proto)
    {
        proto = null;

        // Spawn menu
        if (_placement.CurrentPermission?.EntityType is { } entityType)
            return _proto.TryIndex(entityType, out proto);

        // Construction
        if (_placement is not PlacementManager { Hijack: ConstructionPlacementHijack hijack })
            return false;

        return hijack.CurrentPrototype?.ID is { } recipeId
            && _construction.TryGetRecipePrototype(recipeId, out var entityProtoId)
            && _proto.TryIndex(entityProtoId, out proto);
    }
}
