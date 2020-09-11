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
    public class VentVisualizer : AppearanceVisualizer
    {
        private RSI _ventRSI;

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            var rsiString = node.GetNode("ventRSI").ToString();
            var rsiPath = SharedSpriteComponent.TextureRoot / rsiString;
            try
            {
                var resourceCache = IoCManager.Resolve<IResourceCache>();
                var resource = resourceCache.GetResource<RSIResource>(rsiPath);
                _ventRSI = resource.RSI;
            }
            catch (Exception e)
            {
                Logger.ErrorS("go.ventvisualizer", "Unable to load RSI '{0}'. Trace:\n{1}", rsiPath, e);
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent(out ISpriteComponent sprite))
            {
                return;
            }
            if (!component.TryGetData(VentVisuals.VisualState, out VentVisualState ventVisualState))
            {
                return;
            }

            var ventBaseState = "vent";
            ventBaseState += ventVisualState.VentEnabled ? "On" : "Off";

            sprite.LayerMapReserveBlank(Layer.VentBase);
            var baseVentLayer = sprite.LayerMapGet(Layer.VentBase);
            sprite.LayerSetRSI(baseVentLayer, _ventRSI);
            sprite.LayerSetState(baseVentLayer, ventBaseState);
            sprite.LayerSetVisible(baseVentLayer, true);
        }

        private enum Layer
        {
            VentBase,
        }
    }
}
