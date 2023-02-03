using Content.Shared.MachineLinking;
using Robust.Client.GameObjects;

namespace Content.Client.MachineLinking;

public sealed class SignalSwitchVisualizerSystem : VisualizerSystem<SignalSwitchVisualizerComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SignalSwitchVisualizerComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, SignalSwitchVisualizerComponent comp, ComponentInit args)
    {
        if (TryComp<SpriteComponent>(uid, out var sprite))
            sprite.LayerMapReserveBlank(comp.Layer);
    }

    protected override void OnAppearanceChange(EntityUid uid, SignalSwitchVisualizerComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if(!AppearanceSystem.TryGetData(uid, SignalSwitchVisuals.On, out bool on, args.Component))
            return;

        args.Sprite.LayerSetState(0, on ? "on" : "off");
    }
}
