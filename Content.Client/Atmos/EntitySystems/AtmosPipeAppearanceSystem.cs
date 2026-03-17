using Content.Client.SubFloor;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Atmos.Piping;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Atmos.EntitySystems;

[UsedImplicitly]
public sealed partial class AtmosPipeAppearanceSystem : SharedAtmosPipeAppearanceSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

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

        var numberOfPipeLayers = GetNumberOfPipeLayers(uid, out _);

        foreach (var layerKey in Enum.GetValues<PipeConnectionLayer>())
        {
            for (byte i = 0; i < numberOfPipeLayers; i++)
            {
                var layerName = layerKey.ToString() + i.ToString();
                var layer = _sprite.LayerMapReserve((uid, sprite), layerName);
                _sprite.LayerSetRsi((uid, sprite), layer, component.Sprite[i].RsiPath);
                _sprite.LayerSetRsiState((uid, sprite), layer, component.Sprite[i].RsiState);
                _sprite.LayerSetDirOffset((uid, sprite), layer, ToOffset(layerKey));
            }
        }
    }

    private void HideAllPipeConnection(Entity<SpriteComponent> entity, AtmosPipeLayersComponent? atmosPipeLayers, int numberOfPipeLayers)
    {
        var sprite = entity.Comp;

        foreach (var layerKey in Enum.GetValues<PipeConnectionLayer>())
        {
            for (byte i = 0; i < numberOfPipeLayers; i++)
            {
                var layerName = layerKey.ToString() + i.ToString();

                if (!_sprite.LayerMapTryGet(entity.AsNullable(), layerName, out var key, false))
                    continue;

                var layer = sprite[key];
                layer.Visible = false;
            }
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

        var numberOfPipeLayers = GetNumberOfPipeLayers(uid, out var atmosPipeLayers);

        if (!_appearance.TryGetData<int>(uid, PipeVisuals.VisualState, out var worldConnectedDirections, args.Component))
        {
            HideAllPipeConnection((uid, args.Sprite), atmosPipeLayers, numberOfPipeLayers);
            return;
        }

        if (!_appearance.TryGetData<Color>(uid, PipeColorVisuals.Color, out var color, args.Component))
            color = Color.White;

        for (byte i = 0; i < numberOfPipeLayers; i++)
        {
            // Extract the cardinal pipe orientations for the current pipe layer
            // '15' is the four bit mask that is used to extract the pipe orientations of interest from 'worldConnectedDirections'
            // Fun fact: a collection of four bits is called a 'nibble'! They aren't natively supported :(
            var pipeLayerConnectedDirections = (PipeDirection)(15 & (worldConnectedDirections >> (PipeDirectionHelpers.PipeDirections * i)));

            // Transform the connected directions to local-coordinates
            var connectedDirections = pipeLayerConnectedDirections.RotatePipeDirection(-Transform(uid).LocalRotation);

            foreach (var layerKey in Enum.GetValues<PipeConnectionLayer>())
            {
                var layerName = layerKey.ToString() + i.ToString();

                if (!_sprite.LayerMapTryGet((uid, args.Sprite), layerName, out var key, false))
                    continue;

                var layer = args.Sprite[key];
                var dir = (PipeDirection)layerKey;
                var visible = connectedDirections.HasDirection(dir);

                layer.Visible &= visible;

                if (!visible)
                    continue;

                layer.Color = color;
            }
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
