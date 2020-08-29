using Content.Shared.GameObjects.Atmos;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Disposal
{
    [UsedImplicitly]
    public class PumpVisualizer : AppearanceVisualizer
    {
        private RSI _pumpRSI;

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);
            var rsiString = node.GetNode("pumpRSI").ToString(); //how to load an RSI from a string? I need the appearance to take a different rsi than what the sprite component started as

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
            pumpBaseState += pumpVisualState.OutletDirection.ToString();
            pumpBaseState += pumpVisualState.InletConduitLayer.ToString();
            pumpBaseState += pumpVisualState.OutletConduitLayer.ToString();

            var layer = sprite.AddLayerState(pumpBaseState);

            sprite.LayerMapSet(Layer.PumpBase, layer);
            sprite.LayerSetRSI(layer, _pumpRSI);
        }

        private enum Layer
        {
            PumpBase
        }
    }
}
