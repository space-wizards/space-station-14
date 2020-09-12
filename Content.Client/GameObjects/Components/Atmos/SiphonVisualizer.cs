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
    public class SiphonVisualizer : AppearanceVisualizer
    {
        private RSI _siphonRSI;

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            var rsiString = node.GetNode("siphonRSI").ToString();
            var rsiPath = SharedSpriteComponent.TextureRoot / rsiString;
            try
            {
                var resourceCache = IoCManager.Resolve<IResourceCache>();
                var resource = resourceCache.GetResource<RSIResource>(rsiPath);
                _siphonRSI = resource.RSI;
            }
            catch (Exception e)
            {
                Logger.ErrorS("go.siphonvisualizer", "Unable to load RSI '{0}'. Trace:\n{1}", rsiPath, e);
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent(out ISpriteComponent sprite))
            {
                return;
            }
            if (!component.TryGetData(SiphonVisuals.VisualState, out SiphonVisualState siphonVisualState))
            {
                return;
            }

            var siphonBaseState = "scrub";
            siphonBaseState += siphonVisualState.SiphonEnabled ? "On" : "Off";

            sprite.LayerMapReserveBlank(Layer.SiphonBase);
            var baseSiphonLayer = sprite.LayerMapGet(Layer.SiphonBase);
            sprite.LayerSetRSI(baseSiphonLayer, _siphonRSI);
            sprite.LayerSetState(baseSiphonLayer, siphonBaseState);
            sprite.LayerSetVisible(baseSiphonLayer, true);
        }

        private enum Layer
        {
            SiphonBase,
        }
    }
}
