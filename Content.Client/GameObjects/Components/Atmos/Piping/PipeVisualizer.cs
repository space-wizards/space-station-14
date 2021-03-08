#nullable enable
using System;
using Content.Shared.GameObjects.Components.Atmos;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.GameObjects.Components.Atmos.Piping
{
    /// <summary>
    ///     Sets the state of the sprite based on what shape of pipe it is.
    /// </summary>
    [UsedImplicitly]
    [DataDefinition]
    public class PipeVisualizer : AppearanceVisualizer, ISerializationHooks
    {
        [DataField("rsi")] private string _rsiString = "Constructible/Atmos/pipe.rsi";
        private RSI? _pipeRSI;

        void ISerializationHooks.AfterDeserialization()
        {
            var rsiPath = SharedSpriteComponent.TextureRoot / _rsiString;
            var resourceCache = IoCManager.Resolve<IResourceCache>();

            if (resourceCache.TryGetResource(rsiPath, out RSIResource? rsi))
            {
                _pipeRSI = rsi.RSI;
            }
        }

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);
            if (!entity.TryGetComponent<ISpriteComponent>(out var sprite)) return;
            sprite.LayerMapReserveBlank(Layer.PipeBase);
            var pipeBaseLayer = sprite.LayerMapGet(Layer.PipeBase);

            if (_pipeRSI != null)
                sprite.LayerSetRSI(pipeBaseLayer, _pipeRSI);

            sprite.LayerSetVisible(pipeBaseLayer, true);
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);
            if (!component.Owner.TryGetComponent<ISpriteComponent>(out var sprite)) return;
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
