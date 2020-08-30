using Content.Shared.GameObjects.Components.Atmos;
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
using System.Collections.Generic;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Atmos
{
    [UsedImplicitly]
    public class PipeVisualizer : AppearanceVisualizer
    {
        private RSI _pipeRSI;

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            var rsiString = node.GetNode("pipeRSI").ToString();
            var rsiPath = SharedSpriteComponent.TextureRoot / rsiString;
            try
            {
                var resourceCache = IoCManager.Resolve<IResourceCache>();
                var resource = resourceCache.GetResource<RSIResource>(rsiPath);
                _pipeRSI = resource.RSI;
            }
            catch (Exception e)
            {
                Logger.ErrorS("go.pipevisualizer", "Unable to load RSI '{0}'. Trace:\n{1}", rsiPath, e);
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent(out ISpriteComponent sprite))
            {
                return;
            }
            if (!component.TryGetData(PipeVisuals.VisualState, out PipeVisualStateSet pipeVisualStateSet))
            {
                return;
            }
            for (var i = 0; i < pipeVisualStateSet.PipeVisualStates.Length; i++)
            {
                var pipeVisualState = pipeVisualStateSet.PipeVisualStates[i];
                var rsiState = "pipe";
                rsiState += pipeVisualState.PipeDirection.ToString();
                rsiState += ((int) pipeVisualState.ConduitLayer).ToString();

                var pipeLayerKey = "pipeLayer" + i.ToString();
                sprite.LayerMapReserveBlank(pipeLayerKey);
                var currentPipeLayer = sprite.LayerMapGet(pipeLayerKey);
                sprite.LayerSetRSI(currentPipeLayer, _pipeRSI);
                sprite.LayerSetState(currentPipeLayer, rsiState);
                sprite.LayerSetVisible(currentPipeLayer, true);
            }
        }
    }
}
