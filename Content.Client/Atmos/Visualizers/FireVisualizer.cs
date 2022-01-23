using Content.Shared.Atmos;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Atmos.Visualizers
{
    [UsedImplicitly]
    public class FireVisualizer : AppearanceVisualizer
    {
        [DataField("fireStackAlternateState")]
        private int _fireStackAlternateState = 3;

        [DataField("normalState")]
        private string? _normalState;

        [DataField("alternateState")]
        private string? _alternateState;

        [DataField("sprite")]
        private string? _sprite;

        public override void InitializeEntity(EntityUid entity)
        {
            base.InitializeEntity(entity);

            var sprite = IoCManager.Resolve<IEntityManager>().GetComponent<ISpriteComponent>(entity);

            sprite.LayerMapReserveBlank(FireVisualLayers.Fire);
            sprite.LayerSetVisible(FireVisualLayers.Fire, false);
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (component.TryGetData(FireVisuals.OnFire, out bool onFire))
            {
                var fireStacks = 0f;

                if (component.TryGetData(FireVisuals.FireStacks, out float stacks))
                    fireStacks = stacks;

                SetOnFire(component, onFire, fireStacks);
            }
        }

        private void SetOnFire(AppearanceComponent component, bool onFire, float fireStacks)
        {
            var sprite = IoCManager.Resolve<IEntityManager>().GetComponent<ISpriteComponent>(component.Owner);

            if (_sprite != null)
            {
                sprite.LayerSetRSI(FireVisualLayers.Fire, _sprite);
            }

            sprite.LayerSetVisible(FireVisualLayers.Fire, onFire);

            if(fireStacks > _fireStackAlternateState && !string.IsNullOrEmpty(_alternateState))
                sprite.LayerSetState(FireVisualLayers.Fire, _alternateState);
            else
                sprite.LayerSetState(FireVisualLayers.Fire, _normalState);
        }
    }

    public enum FireVisualLayers : byte
    {
        Fire
    }
}
