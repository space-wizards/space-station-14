using Content.Shared.Conveyor;
using Content.Shared.Recycling;
using Robust.Client.GameObjects;

namespace Content.Client.Recycling;

public sealed class RecyclerVisualizerSystem : VisualizerSystem<RecyclerVisualizerComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RecyclerVisualizerComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, RecyclerVisualizerComponent comp, ComponentInit args)
    {
        if (!TryComp(uid, out SpriteComponent? sprite) ||
            !TryComp(uid, out AppearanceComponent? appearance))
        {
            return;
        }

        UpdateAppearance(uid, comp, appearance, sprite);
    }

    protected override void OnAppearanceChange(EntityUid uid, RecyclerVisualizerComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        UpdateAppearance(uid, comp, args.Component, args.Sprite);
    }

    private void UpdateAppearance(EntityUid uid, RecyclerVisualizerComponent comp, AppearanceComponent appearance, SpriteComponent sprite)
    {
        var state = comp.StateOff;
        if (AppearanceSystem.TryGetData(uid, ConveyorVisuals.State, out ConveyorState conveyorState, appearance) && conveyorState != ConveyorState.Off)
        {
            state = comp.StateOn;
        }

        if (AppearanceSystem.TryGetData(uid, RecyclerVisuals.Bloody, out bool bloody, appearance) && bloody)
        {
            state += "bld";
        }

        sprite.LayerSetState(RecyclerVisualLayers.Main, state);
    }
}

public enum RecyclerVisualLayers : byte
{
    Main
}
