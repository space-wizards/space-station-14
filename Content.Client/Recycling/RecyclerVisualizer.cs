using Content.Shared.Recycling;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Recycling
{
    [UsedImplicitly]
    public class RecyclerVisualizer : AppearanceVisualizer
    {
        [DataField("state_clean")]
        private string? _stateClean;

        [DataField("state_bloody")]
        private string? _stateBloody;

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            if (!entity.TryGetComponent(out ISpriteComponent? sprite) ||
                !entity.TryGetComponent(out AppearanceComponent? appearance))
            {
                return;
            }

            appearance.TryGetData(RecyclerVisuals.Bloody, out bool bloody);
            sprite.LayerSetState(RecyclerVisualLayers.Bloody, bloody
                ? _stateBloody
                : _stateClean);
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent(out ISpriteComponent? sprite))
            {
                return;
            }

            component.TryGetData(RecyclerVisuals.Bloody, out bool bloody);
            sprite.LayerSetState(RecyclerVisualLayers.Bloody, bloody
                ? _stateBloody
                : _stateClean);
        }
    }

    public enum RecyclerVisualLayers : byte
    {
        Bloody
    }
}
