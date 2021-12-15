using Content.Shared.APC;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Power.APC
{
    public class ApcVisualizer : AppearanceVisualizer
    {
        [UsedImplicitly]
        public override void InitializeEntity(EntityUid entity)
        {
            base.InitializeEntity(entity);

            var sprite = IoCManager.Resolve<IEntityManager>().GetComponent<ISpriteComponent>(entity);

            sprite.LayerMapSet(Layers.ChargeState, sprite.AddLayerState("apco3-0"));
            sprite.LayerSetShader(Layers.ChargeState, "unshaded");

            sprite.LayerMapSet(Layers.Lock, sprite.AddLayerState("apcox-0"));
            sprite.LayerSetShader(Layers.Lock, "unshaded");

            sprite.LayerMapSet(Layers.Equipment, sprite.AddLayerState("apco0-3"));
            sprite.LayerSetShader(Layers.Equipment, "unshaded");

            sprite.LayerMapSet(Layers.Lighting, sprite.AddLayerState("apco1-3"));
            sprite.LayerSetShader(Layers.Lighting, "unshaded");

            sprite.LayerMapSet(Layers.Environment, sprite.AddLayerState("apco2-3"));
            sprite.LayerSetShader(Layers.Environment, "unshaded");
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var sprite = IoCManager.Resolve<IEntityManager>().GetComponent<ISpriteComponent>(component.Owner);
            if (component.TryGetData<ApcChargeState>(ApcVisuals.ChargeState, out var state))
            {
                switch (state)
                {
                    case ApcChargeState.Lack:
                        sprite.LayerSetState(Layers.ChargeState, "apco3-0");
                        break;
                    case ApcChargeState.Charging:
                        sprite.LayerSetState(Layers.ChargeState, "apco3-1");
                        break;
                    case ApcChargeState.Full:
                        sprite.LayerSetState(Layers.ChargeState, "apco3-2");
                        break;
                }
            }
            else
            {
                sprite.LayerSetState(Layers.ChargeState, "apco3-0");
            }
        }

        enum Layers : byte
        {
            ChargeState,
            Lock,
            Equipment,
            Lighting,
            Environment,
        }
    }
}
