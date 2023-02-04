using Content.Client.AME.Components;
using Robust.Client.GameObjects;
using static Content.Shared.AME.SharedAMEShieldComponent;

namespace Content.Client.AME;

public sealed class AMEShieldingVisualizerSystem : VisualizerSystem<AMEShieldingVisualsComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AMEShieldingVisualsComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(EntityUid uid, AMEShieldingVisualsComponent component, ComponentInit args)
    {
        if (TryComp<SpriteComponent>(uid, out var sprite))
        {
            sprite.LayerMapSet(AMEShieldingVisualsLayer.Core, sprite.AddLayerState("core"));
            sprite.LayerSetVisible(AMEShieldingVisualsLayer.Core, false);
            sprite.LayerMapSet(AMEShieldingVisualsLayer.CoreState, sprite.AddLayerState("core_weak"));
            sprite.LayerSetVisible(AMEShieldingVisualsLayer.CoreState, false);
        }
    }

    protected override void OnAppearanceChange(EntityUid uid, AMEShieldingVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (AppearanceSystem.TryGetData<string>(uid, AMEShieldVisuals.Core, out var core, args.Component))
        {
            if (core == "isCore")
            {
                args.Sprite.LayerSetState(AMEShieldingVisualsLayer.Core, "core");
                args.Sprite.LayerSetVisible(AMEShieldingVisualsLayer.Core, true);

            }
            else
            {
                args.Sprite.LayerSetVisible(AMEShieldingVisualsLayer.Core, false);
            }
        }

        if (AppearanceSystem.TryGetData<string>(uid, AMEShieldVisuals.CoreState, out var coreState, args.Component))
        {
            switch (coreState)
            {
                case "weak":
                    args.Sprite.LayerSetState(AMEShieldingVisualsLayer.CoreState, "core_weak");
                    args.Sprite.LayerSetVisible(AMEShieldingVisualsLayer.CoreState, true);
                    break;
                case "strong":
                    args.Sprite.LayerSetState(AMEShieldingVisualsLayer.CoreState, "core_strong");
                    args.Sprite.LayerSetVisible(AMEShieldingVisualsLayer.CoreState, true);
                    break;
                case "off":
                    args.Sprite.LayerSetVisible(AMEShieldingVisualsLayer.CoreState, false);
                    break;
            }
        }
    }
}

public enum AMEShieldingVisualsLayer : byte
{
    Core,
    CoreState,
}
