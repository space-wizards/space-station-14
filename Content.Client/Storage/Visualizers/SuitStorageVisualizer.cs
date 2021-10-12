using Content.Shared.Storage;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Storage.Visualizers
{
    [UsedImplicitly]
    public sealed class SuitStorageVisualizer : AppearanceVisualizer
    {
        public override void InitializeEntity(IEntity entity)
        {
            if (!entity.TryGetComponent(out ISpriteComponent? sprite))
            {
                return;
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent(out ISpriteComponent? sprite))
            {
                return;
            }

            // Checks if the door is open and if so, unhides the "open" layer
            component.TryGetData(SuitStorageVisuals.Open, out bool open);
            sprite.LayerSetVisible(SuitStorageVisualLayers.Opening, open);

            component.TryGetData(SuitStorageVisuals.ContainsHelmet, out bool helmet);
            sprite.LayerSetVisible(SuitStorageVisualLayers.Helmet, helmet);

            component.TryGetData(SuitStorageVisuals.ContainsSuit, out bool suit);
            sprite.LayerSetVisible(SuitStorageVisualLayers.Suit, suit);

            component.TryGetData(SuitStorageVisuals.ContainsBoots, out bool boots);
            sprite.LayerSetVisible(SuitStorageVisualLayers.Boots, boots);

        }
    }

    public enum SuitStorageVisualLayers : byte
    {
        Base,
        Opening,
        Helmet,
        Suit,
        Boots
    }
}
