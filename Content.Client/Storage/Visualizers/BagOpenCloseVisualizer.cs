
using Content.Shared.Stacks;
using Content.Shared.Storage;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Client.Storage.Visualizers
{
    [UsedImplicitly]
    public class BagOpenCloseVisualizer : AppearanceVisualizer, ISerializationHooks
    {
        private const string OpenIcon = "openIcon";
        [DataField(OpenIcon)]
        private string? _openIcon;

        void ISerializationHooks.AfterDeserialization()
        {
            if(_openIcon == null){
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
