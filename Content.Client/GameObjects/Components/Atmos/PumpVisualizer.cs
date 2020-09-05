using Content.Shared.GameObjects.Atmos;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects.Components.Renderable;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;
using System;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Atmos
{
    [UsedImplicitly]
    public class PumpVisualizer : AppearanceVisualizer
    {
        private RSI _pumpRSI;

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            var rsiString = node.GetNode("pumpRSI").ToString();
            var rsiPath = SharedSpriteComponent.TextureRoot / rsiString;
            try
            {
                var resourceCache = IoCManager.Resolve<IResourceCache>();
                var resource = resourceCache.GetResource<RSIResource>(rsiPath);
                _pumpRSI = resource.RSI;
            }
            catch (Exception e)
            {
                Logger.ErrorS("go.pumpvisualizer", "Unable to load RSI '{0}'. Trace:\n{1}", rsiPath, e);
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent(out ISpriteComponent sprite))
            {
                return;
            }
            if (!component.TryGetData(PumpVisuals.VisualState, out PumpVisualState pumpVisualState))
            {
                return;
            }
            var pumpBaseState = "pump";
            pumpBaseState += pumpVisualState.InletDirection.ToString();
            pumpBaseState += ((int) pumpVisualState.InletConduitLayer).ToString();
            pumpBaseState += pumpVisualState.OutletDirection.ToString();
            pumpBaseState += ((int) pumpVisualState.OutletConduitLayer).ToString();

            sprite.LayerMapReserveBlank(Layer.PumpBase);
            var basePumpLayer = sprite.LayerMapGet(Layer.PumpBase);
            sprite.LayerSetRSI(basePumpLayer, _pumpRSI);
            sprite.LayerSetState(basePumpLayer, pumpBaseState);
            sprite.LayerSetVisible(basePumpLayer, true);



            var pumpEnabledAnimationState = "pumpEnabled";
            pumpEnabledAnimationState += pumpVisualState.InletDirection.ToString();
            pumpEnabledAnimationState += ((int) pumpVisualState.InletConduitLayer).ToString();
            pumpEnabledAnimationState += pumpVisualState.OutletDirection.ToString();
            pumpEnabledAnimationState += ((int) pumpVisualState.OutletConduitLayer).ToString();

            sprite.LayerMapReserveBlank(Layer.PumpEnabled);
            var pumpEnabledAnimationLayer = sprite.LayerMapGet(Layer.PumpEnabled);
            sprite.LayerSetRSI(pumpEnabledAnimationLayer, _pumpRSI);
            sprite.LayerSetState(pumpEnabledAnimationLayer, pumpEnabledAnimationState);
            sprite.LayerSetVisible(pumpEnabledAnimationLayer, pumpVisualState.PumpEnabled);
        }

        private enum Layer
        {
            PumpBase,
            PumpEnabled,
        }
    }
}
