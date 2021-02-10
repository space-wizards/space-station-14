using Content.Shared.GameObjects.Components.Atmos;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Atmos
{
    [UsedImplicitly]
    public class GasAnalyzerVisualizer : AppearanceVisualizer
    {
        private string _stateOff;
        private string _stateWorking;

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            _stateOff = node.GetNode("state_off").AsString();
            _stateWorking = node.GetNode("state_working").AsString();
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (component.Deleted)
            {
                return;
            }

            if (!component.Owner.TryGetComponent(out ISpriteComponent sprite))
            {
                return;
            }

            if (component.TryGetData(GasAnalyzerVisuals.VisualState, out GasAnalyzerVisualState visualState))
            {
                switch (visualState)
                {
                    case GasAnalyzerVisualState.Off:
                        sprite.LayerSetState(0, _stateOff);
                        break;
                    case GasAnalyzerVisualState.Working:
                        sprite.LayerSetState(0, _stateWorking);
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
