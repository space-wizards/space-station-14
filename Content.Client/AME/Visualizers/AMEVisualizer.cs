using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using static Content.Shared.AME.SharedAMEShieldComponent;

namespace Content.Client.AME.Visualizers
{
    [UsedImplicitly]
    public class AMEVisualizer : AppearanceVisualizer
    {
        public override void InitializeEntity(EntityUid entity)
        {
            base.InitializeEntity(entity);
            var sprite = IoCManager.Resolve<IEntityManager>().GetComponent<ISpriteComponent>(entity);
            sprite.LayerMapSet(Layers.Core, sprite.AddLayerState("core"));
            sprite.LayerSetVisible(Layers.Core, false);
            sprite.LayerMapSet(Layers.CoreState, sprite.AddLayerState("core_weak"));
            sprite.LayerSetVisible(Layers.CoreState, false);
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);
            var sprite = IoCManager.Resolve<IEntityManager>().GetComponent<ISpriteComponent>(component.Owner);
            if (component.TryGetData<string>(AMEShieldVisuals.Core, out var core))
            {
                if (core == "isCore")
                {
                    sprite.LayerSetState(Layers.Core, "core");
                    sprite.LayerSetVisible(Layers.Core, true);
                }
                else
                {
                    sprite.LayerSetVisible(Layers.Core, false);
                }
            }

            if (component.TryGetData<string>(AMEShieldVisuals.CoreState, out var coreState))
                switch (coreState)
                {
                    case "weak":
                        sprite.LayerSetState(Layers.CoreState, "core_weak");
                        sprite.LayerSetVisible(Layers.CoreState, true);
                        break;
                    case "strong":
                        sprite.LayerSetState(Layers.CoreState, "core_strong");
                        sprite.LayerSetVisible(Layers.CoreState, true);
                        break;
                    case "off":
                        sprite.LayerSetVisible(Layers.CoreState, false);
                        break;
                }
        }
    }

    enum Layers : byte
    {
        Core,
        CoreState,
    }
}
