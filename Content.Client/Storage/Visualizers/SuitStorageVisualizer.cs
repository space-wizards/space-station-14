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

        }
    }

    public enum SuitStorageVisualLayers : byte
    {
        Base,
        Opening
    }
}
