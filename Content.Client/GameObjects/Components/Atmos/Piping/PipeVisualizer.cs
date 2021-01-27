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
using Robust.Shared.Serialization;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Atmos
{
    /// <summary>
    ///     Sets the state of the sprite based on what shape of pipe it is.
    /// </summary>
    [UsedImplicitly]
    public class PipeVisualizer : AppearanceVisualizer
    {
        private RSI _pipeRSI;

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);
            var serializer = YamlObjectSerializer.NewReader(node);

            var rsiString = serializer.ReadDataField("rsi", "Constructible/Atmos/pipe.rsi");
            if (!string.IsNullOrWhiteSpace(rsiString))
            {
                var rsiPath = SharedSpriteComponent.TextureRoot / rsiString;
                try
                {
                    _pipeRSI = IoCManager.Resolve<IResourceCache>().GetResource<RSIResource>(rsiPath).RSI;
                }
                catch (Exception e)
                {
                    Logger.ErrorS($"{nameof(PipeVisualizer)}", $"Unable to load RSI {rsiPath}. Trace:\n{e}");
                }
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
            stateId += pipeVisualState.PipeShape.ToString();
            return stateId;
        }

        private enum Layer : byte
        {
            PipeBase,
        }
    }
}
