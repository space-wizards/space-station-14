using Content.Shared.GameObjects.Components.Atmos;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using System.Collections.Generic;

namespace Content.Client.GameObjects.Components.Atmos
{
    [UsedImplicitly]
    public class PipeVisualizer : AppearanceVisualizer
    {
        private readonly List<object> _pipeLayerKeys = new List<object>();

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
                if (!_pipeLayerKeys.Contains(pipeLayerKey))
                {
                    _pipeLayerKeys.Add(pipeLayerKey);
                    sprite.LayerMapSet(pipeLayerKey, sprite.AddLayerState(rsiState));
                }
                else
                {
                    var layer = sprite.LayerMapGet(pipeLayerKey);
                    sprite.LayerSetState(layer, rsiState);
                }
            }
        }
    }
}
