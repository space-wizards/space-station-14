using Content.Shared.Power;
using Content.Shared.SMES;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Power.SMES
{
    [UsedImplicitly]
    public sealed class SmesVisualizer : AppearanceVisualizer
    {
        [Obsolete("Subscribe to your component being initialised instead.")]
        public override void InitializeEntity(EntityUid entity)
        {
            base.InitializeEntity(entity);

            var sprite = IoCManager.Resolve<IEntityManager>().GetComponent<SpriteComponent>(entity);

            sprite.LayerMapSet(Layers.Input, sprite.AddLayerState("smes-oc0"));
            sprite.LayerSetShader(Layers.Input, "unshaded");
            sprite.LayerMapSet(Layers.Charge, sprite.AddLayerState("smes-og1"));
            sprite.LayerSetShader(Layers.Charge, "unshaded");
            sprite.LayerSetVisible(Layers.Charge, false);
            sprite.LayerMapSet(Layers.Output, sprite.AddLayerState("smes-op0"));
            sprite.LayerSetShader(Layers.Output, "unshaded");
        }

        [Obsolete("Subscribe to AppearanceChangeEvent instead.")]
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var sprite = IoCManager.Resolve<IEntityManager>().GetComponent<SpriteComponent>(component.Owner);
            if (!component.TryGetData<int>(SmesVisuals.LastChargeLevel, out var level) || level == 0)
            {
                sprite.LayerSetVisible(Layers.Charge, false);
            }
            else
            {
                sprite.LayerSetVisible(Layers.Charge, true);
                sprite.LayerSetState(Layers.Charge, $"smes-og{level}");
            }

            if (component.TryGetData<ChargeState>(SmesVisuals.LastChargeState, out var state))
            {
                switch (state)
                {
                    case ChargeState.Still:
                        sprite.LayerSetState(Layers.Input, "smes-oc0");
                        sprite.LayerSetState(Layers.Output, "smes-op1");
                        break;
                    case ChargeState.Charging:
                        sprite.LayerSetState(Layers.Input, "smes-oc1");
                        sprite.LayerSetState(Layers.Output, "smes-op1");
                        break;
                    case ChargeState.Discharging:
                        sprite.LayerSetState(Layers.Input, "smes-oc0");
                        sprite.LayerSetState(Layers.Output, "smes-op2");
                        break;
                }
            }
            else
            {
                sprite.LayerSetState(Layers.Input, "smes-oc0");
                sprite.LayerSetState(Layers.Output, "smes-op1");
            }
        }

        enum Layers : byte
        {
            Input,
            Charge,
            Output,
        }
    }
}
