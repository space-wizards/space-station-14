#nullable enable

using Content.Shared.GameObjects.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Client.GameObjects.Components.Storage
{
    [UsedImplicitly]
    public class BagOpenCloseVisualizer : AppearanceVisualizer
    {
        private const string OpenIcon = "openIcon";
        private string? _openIcon;

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            if (node.TryGetNode<YamlScalarNode>(OpenIcon, out var openIconNode))
            {
                _openIcon = openIconNode.Value;
            }
            else
            {
                Logger.Warning("BagOpenCloseVisualizer is useless with no `openIcon`");
            }
        }

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            if (_openIcon != null &&
                entity.TryGetComponent<SpriteComponent>(out var spriteComponent) &&
                spriteComponent.BaseRSI?.Path != null)
            {
                spriteComponent.LayerMapReserveBlank(OpenIcon);
                spriteComponent.LayerSetSprite(OpenIcon, new Rsi(spriteComponent.BaseRSI.Path, _openIcon));
                spriteComponent.LayerSetVisible(OpenIcon, false);
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (_openIcon != null
                && component.Owner.TryGetComponent<SpriteComponent>(out var spriteComponent))
            {
                if (component.TryGetData<SharedBagState>(SharedBagOpenVisuals.BagState, out var bagState))
                {
                    switch (bagState)
                    {
                        case SharedBagState.Open:
                            spriteComponent.LayerSetVisible(OpenIcon, true);
                            break;
                        default:
                            spriteComponent.LayerSetVisible(OpenIcon, false);
                            break;
                    }
                    component.SetData(StackVisuals.Hide, bagState == SharedBagState.Close);
                }
            }
        }
    }
}
