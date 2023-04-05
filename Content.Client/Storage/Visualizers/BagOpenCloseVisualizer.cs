using Content.Shared.Storage.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Client.Storage.Visualizers
{
    [UsedImplicitly]
    public sealed class BagOpenCloseVisualizer : AppearanceVisualizer, ISerializationHooks
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

        [Obsolete("Subscribe to your component being initialised instead.")]
        public override void InitializeEntity(EntityUid entity)
        {
            base.InitializeEntity(entity);

            var entities = IoCManager.Resolve<IEntityManager>();

            if (_openIcon != null &&
                entities.TryGetComponent<SpriteComponent?>(entity, out var spriteComponent) &&
                spriteComponent.BaseRSI?.Path is { } path)
            {
                spriteComponent.LayerMapReserveBlank(OpenIcon);
                spriteComponent.LayerSetSprite(OpenIcon, new Rsi(path, _openIcon));
                spriteComponent.LayerSetVisible(OpenIcon, false);
            }
        }

        [Obsolete("Subscribe to AppearanceChangeEvent instead.")]
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var entities = IoCManager.Resolve<IEntityManager>();

            if (_openIcon == null ||
                !entities.TryGetComponent(component.Owner, out SpriteComponent? spriteComponent))
                return;

            if (!component.TryGetData<SharedBagState>(SharedBagOpenVisuals.BagState, out var bagState))
                return;

            switch (bagState)
            {
                case SharedBagState.Open:
                    spriteComponent.LayerSetVisible(OpenIcon, true);
                    break;
                default:
                    spriteComponent.LayerSetVisible(OpenIcon, false);
                    break;
            }
        }
    }
}
