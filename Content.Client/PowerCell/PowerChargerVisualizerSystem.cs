using Content.Shared.Power;
using Robust.Client.GameObjects;

namespace Content.Client.PowerCell;

public sealed class PowerChargerVisualizerSystem : VisualizerSystem<PowerChargerVisualizerComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PowerChargerVisualizerComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, PowerChargerVisualizerComponent comp, ComponentInit args)
    {
        if(!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        // Base item
        sprite.LayerMapSet(Layers.Base, sprite.AddLayerState("empty"));

        // Light
        sprite.LayerMapSet(Layers.Light, sprite.AddLayerState("light-off"));
        sprite.LayerSetShader(Layers.Light, "unshaded");
    }

    protected override void OnAppearanceChange(EntityUid uid, PowerChargerVisualizerComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        // Update base item
        if (AppearanceSystem.TryGetData(uid, CellVisual.Occupied, out bool occupied, args.Component))
        {
            // TODO: don't throw if it doesn't have a full state
            args.Sprite.LayerSetState(Layers.Base, occupied ? "full" : "empty");
        }
        else
        {
            args.Sprite.LayerSetState(Layers.Base, "empty");
        }

        // Update lighting
        if (AppearanceSystem.TryGetData(uid, CellVisual.Light, out CellChargerStatus status, args.Component))
        {
            switch (status)
            {
                case CellChargerStatus.Off:
                    args.Sprite.LayerSetState(Layers.Light, "light-off");
                    break;
                case CellChargerStatus.Empty:
                    args.Sprite.LayerSetState(Layers.Light, "light-empty");
                    break;
                case CellChargerStatus.Charging:
                    args.Sprite.LayerSetState(Layers.Light, "light-charging");
                    break;
                case CellChargerStatus.Charged:
                    args.Sprite.LayerSetState(Layers.Light, "light-charged");
                    break;
                default:
                    args.Sprite.LayerSetState(Layers.Light, "light-off");
                    break;
            }
        }
        else
        {
            args.Sprite.LayerSetState(Layers.Light, "light-off");
        }
    }
}

enum Layers : byte
{
    Base,
    Light,
}
