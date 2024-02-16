using Content.Shared.Light.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Light;

public abstract class SharedRgbLightControllerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RgbLightControllerComponent, ComponentGetState>(OnGetState);
    }

    private void OnGetState(EntityUid uid, RgbLightControllerComponent component, ref ComponentGetState args)
    {
        args.State = new RgbLightControllerState(component.CycleRate, component.Layers);
    }

    public void SetLayers(EntityUid uid, List<int>? layers,  RgbLightControllerComponent? rgb = null)
    {
        if (!Resolve(uid, ref rgb))
            return;

        rgb.Layers = layers;
        Dirty(rgb);
    }

    public void SetCycleRate(EntityUid uid, float rate, RgbLightControllerComponent? rgb = null)
    {
        if (!Resolve(uid, ref rgb))
            return;

        rgb.CycleRate = Math.Clamp(0.01f, rate, 1); // lets not give people seizures
        Dirty(rgb);
    }
}
