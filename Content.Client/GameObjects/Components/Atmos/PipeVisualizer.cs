using System;
using Content.Shared.GameObjects.Components.Atmos;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects.Components.Renderable;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;
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
                Logger.ErrorS("go.ventvisualizer", "Unable to load RSI '{0}'. Trace:\n{1}", rsiPath, e);
            }
        }

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);
            if (!entity.TryGetComponent(out ISpriteComponent sprite)) return;
            sprite.LayerMapReserveBlank(Layer.PipeBase);
            var pipeBaseLayer = sprite.LayerMapGet(Layer.PipeBase);
            sprite.LayerSetRSI(pipeBaseLayer, _pipeRSI);
            sprite.LayerSetVisible(pipeBaseLayer, true);
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);
            if (!component.Owner.TryGetComponent(out ISpriteComponent sprite)) return;
            if (!component.TryGetData(PipeVisuals.VisualState, out PipeVisualState pipeVisualState)) return;
            var pipeBase = sprite.LayerMapGet(Layer.PipeBase);
            var pipeBaseStateId = GetPipeBaseStateId(pipeVisualState);
            sprite.LayerSetState(pipeBase, pipeBaseStateId);
        }

        private string GetPipeBaseStateId(PipeVisualState pipeVisualState)
        {
            var stateId = "pipe";
            stateId += pipeVisualState.PipeDirection.PipeDirectionToPipeShape().ToString();
            stateId += (int) pipeVisualState.ConduitLayer;
            return stateId;
        }

        private enum Layer
        {
            PipeBase,
        }
    }
}
