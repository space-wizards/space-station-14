using Content.Client.SubFloor;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Atmos.EntitySystems;

[UsedImplicitly]
public sealed class AtmosPipeAppearanceSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PipeAppearanceComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<PipeAppearanceComponent, AppearanceChangeEvent>(OnAppearanceChanged, after: [typeof(SubFloorHideSystem)]);
    }

    private void OnInit(EntityUid uid, PipeAppearanceComponent component, ComponentInit args)
    {
        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        foreach (PipeConnectionLayer layerKey in Enum.GetValues(typeof(PipeConnectionLayer)))
        {
            sprite.LayerMapReserveBlank(layerKey);
            var layer = sprite.LayerMapGet(layerKey);
            sprite.LayerSetRSI(layer, component.Sprite.RsiPath);
            sprite.LayerSetState(layer, component.Sprite.RsiState);
            sprite.LayerSetDirOffset(layer, ToOffset(layerKey));
        }
    }

    private void HideAllPipeConnection(SpriteComponent sprite)
    {
        foreach (PipeConnectionLayer layerKey in Enum.GetValues(typeof(PipeConnectionLayer)))
        {
            if (!sprite.LayerMapTryGet(layerKey, out var key))
                continue;

            var layer = sprite[key];
            layer.Visible = false;
        }
    }

    private void OnAppearanceChanged(EntityUid uid, PipeAppearanceComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!args.Sprite.Visible)
        {
            // This entity is probably below a floor and is not even visible to the user -> don't bother updating sprite data.
            // Note that if the subfloor visuals change, then another AppearanceChangeEvent will get triggered.
            return;
        }

        if (!_appearance.TryGetData<PipeDirection>(uid, PipeVisuals.VisualState, out var worldConnectedDirections, args.Component))
        {
            HideAllPipeConnection(args.Sprite);
            return;
        }

        if (!_appearance.TryGetData<Color>(uid, PipeColorVisuals.Color, out var color, args.Component))
            color = Color.White;

        // transform connected directions to local-coordinates
        var connectedDirections = worldConnectedDirections.RotatePipeDirection(-Transform(uid).LocalRotation);

        foreach (PipeConnectionLayer layerKey in Enum.GetValues(typeof(PipeConnectionLayer)))
        {
            if (!args.Sprite.LayerMapTryGet(layerKey, out var key))
                continue;

            var layer = args.Sprite[key];
            var dir = (PipeDirection) layerKey;
            var visible = connectedDirections.HasDirection(dir);

            layer.Visible &= visible;

            if (!visible)
                continue;

            layer.Color = color;
        }
    }

    private SpriteComponent.DirectionOffset ToOffset(PipeConnectionLayer layer)
    {
        return layer switch
        {
            PipeConnectionLayer.NorthConnection => SpriteComponent.DirectionOffset.Flip,
            PipeConnectionLayer.EastConnection => SpriteComponent.DirectionOffset.CounterClockwise,
            PipeConnectionLayer.WestConnection => SpriteComponent.DirectionOffset.Clockwise,
            _ => SpriteComponent.DirectionOffset.None,
        };
    }

    private enum PipeConnectionLayer : byte
    {
        NorthConnection = PipeDirection.North,
        SouthConnection = PipeDirection.South,
        EastConnection = PipeDirection.East,
        WestConnection = PipeDirection.West,
    }
}
