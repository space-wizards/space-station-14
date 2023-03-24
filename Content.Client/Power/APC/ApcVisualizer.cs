using Content.Shared.APC;
using Robust.Client.GameObjects;

namespace Content.Client.Power.APC
{
    public sealed class ApcVisualizer : AppearanceVisualizer
    {
        public static readonly Color LackColor = Color.FromHex("#d1332e");
        public static readonly Color ChargingColor = Color.FromHex("#2e8ad1");
        public static readonly Color FullColor = Color.FromHex("#3db83b");
        public static readonly Color EmagColor = Color.FromHex("#1f48d6");

        [Obsolete("Subscribe to AppearanceChangeEvent instead.")]
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var ent = IoCManager.Resolve<IEntityManager>();
            var sprite = ent.GetComponent<SpriteComponent>(component.Owner);
            if (component.TryGetData<ApcChargeState>(ApcVisuals.ChargeState, out var chargeState))
            {
                switch (chargeState)
                {
                    case ApcChargeState.Lack:
                        sprite.LayerSetState(ApcVisualLayers.ChargeState, "powered-1");
                        break;
                    case ApcChargeState.Charging:
                        sprite.LayerSetState(ApcVisualLayers.ChargeState, "powered-2");
                        break;
                    case ApcChargeState.Full:
                        sprite.LayerSetState(ApcVisualLayers.ChargeState, "powered-3");
                        break;
                    case ApcChargeState.Emag:
                        sprite.LayerSetState(ApcVisualLayers.ChargeState, "emag-unlit");
                        break;
                }

                if (ent.TryGetComponent(component.Owner, out SharedPointLightComponent? light))
                {
                    light.Color = chargeState switch
                    {
                        ApcChargeState.Lack => LackColor,
                        ApcChargeState.Charging => ChargingColor,
                        ApcChargeState.Full => FullColor,
                        ApcChargeState.Emag => EmagColor,
                        _ => LackColor
                    };
                }
            }
            else
            {
                sprite.LayerSetState(ApcVisualLayers.ChargeState, "powered-1");
            }
        }

        enum ApcVisualLayers : byte
        {
            ChargeState,
            Lock,
            Equipment,
            Lighting,
            Environment,
        }
    }
}
