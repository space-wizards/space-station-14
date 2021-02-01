using Content.Shared.GameObjects.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Nutrition
{
    [UsedImplicitly]
    public class BurnStateVisualizer : AppearanceVisualizer
    {
        private string _burntIcon = "burnt-icon";
        private string _litIcon = "lit-icon";
        private string _unlitIcon = "icon";

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            if (node.TryGetNode("unlitIcon", out var unlitIcon))
            {
                _unlitIcon = unlitIcon.AsString();
            }

            if (node.TryGetNode("litIcon", out var litIcon))
            {
                _litIcon = litIcon.AsString();
            }

            if (node.TryGetNode("burntIcon", out var burntIcon))
            {
                _burntIcon = burntIcon.AsString();
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (component.TryGetData<SharedBurningStates>(SmokingVisuals.Smoking, out var smoking))
            {
                SetState(component, smoking);
            }
        }

        private void SetState(AppearanceComponent component, SharedBurningStates burnState)
        {
            if (component.Owner.TryGetComponent<ISpriteComponent>(out var sprite))
            {
                switch (burnState)
                {
                    case SharedBurningStates.Lit:
                        sprite.LayerSetState(0, _litIcon);
                        break;
                    case SharedBurningStates.Burnt:
                        sprite.LayerSetState(0, _burntIcon);
                        break;
                    default:
                        sprite.LayerSetState(0, _unlitIcon);
                        break;
                }
            }
        }
    }
}