using Content.Shared.Ignitable;
using Content.Shared.Smoking;
using Content.Shared.Light;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Content.Client.Light.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Light
{
    [UsedImplicitly]
    public class CandleStateVisualizer : AppearanceVisualizer
    {
        [DataField("brandNewUnlit")]
        private string _brandNewUnlitIcon = "burnt-icon";
        [DataField("halfNewUnlit")]
        private string _halfNewUnlitIcon = "lit-icon";
        [DataField("almostOutUnlit")]
        private string _almostOutUnlitIcon = "icon";
        [DataField("brandNewlit")]
        private string _brandNewLitIcon = "burnt-icon";
        [DataField("halfNewlit")]
        private string _halfNewLitIcon = "lit-icon";
        [DataField("almostOutlit")]
        private string _almostOutLitIcon = "icon";
        [DataField("dead")]
        private string _deadIcon = "icon";

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (component.TryGetData<CandleState>(CandleVisuals.CandleState, out var candleIconState))
            {
                if (component.TryGetData<SmokableState>(IgnitableVisuals.SmokableState, out var candleSmokableState))
                {
                    SetState(component, candleIconState, candleSmokableState);

                    var entities = IoCManager.Resolve<IEntityManager>();
                    if (component.TryGetData(CandleVisuals.Behaviour, out string lightBehaviourID))
                    {
                        if (entities.TryGetComponent(component.Owner, out LightBehaviourComponent lightBehaviour))
                        {
                            lightBehaviour.StopLightBehaviour();

                            if (candleSmokableState == SmokableState.Lit)
                            {
                                if (lightBehaviourID != string.Empty)
                                    lightBehaviour.StartLightBehaviour(lightBehaviourID);
                            }
                            else if (entities.TryGetComponent(component.Owner, out PointLightComponent light))
                            {
                                light.Enabled = false;
                            }
                        }
                    }
                }
                    
            }
        }

        private void SetState(AppearanceComponent component, CandleState burnState, SmokableState smokeableState)
        {
            var entities = IoCManager.Resolve<IEntityManager>();

            if (entities.TryGetComponent(component.Owner, out ISpriteComponent sprite))
            {
                switch (burnState)
                {
                    case CandleState.BrandNew:
                        if(smokeableState == SmokableState.Lit)
                        {
                            sprite.LayerSetState(0, _brandNewLitIcon);
                            break;
                        }
                            sprite.LayerSetState(0, _brandNewUnlitIcon);
                        break;
                    case CandleState.Half:
                        if (smokeableState == SmokableState.Lit)
                        {
                            sprite.LayerSetState(0, _halfNewLitIcon);
                            break;
                        }
                            sprite.LayerSetState(0, _halfNewUnlitIcon);
                        break;
                    case CandleState.AlmostOut:
                        if (smokeableState == SmokableState.Lit)
                        {
                            sprite.LayerSetState(0, _almostOutLitIcon);
                            break;
                        }
                            sprite.LayerSetState(0, _almostOutUnlitIcon);
                        break;
                    case CandleState.Dead:
                            sprite.LayerSetState(0, _deadIcon);
                        break;
                }
            }
        }
    }
}
