using Content.Shared.Light;
using Content.Shared.PDA;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.PDA
{
    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    public sealed class PDAVisualizer : AppearanceVisualizer
    {
        /// <summary>
        /// The base PDA sprite state, eg. "pda", "pda-clown"
        /// </summary>
        [DataField("state")]
        private string? _state;

        private enum PDAVisualLayers : byte
        {
            Base,
            Flashlight,
            IDLight
        }

        [Obsolete("Subscribe to your component being initialised instead.")]
        public override void InitializeEntity(EntityUid entity)
        {
            base.InitializeEntity(entity);
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var sprite = entityManager.GetComponent<ISpriteComponent>(entity);

            if (_state != null)
            {
                sprite.LayerMapSet(PDAVisualLayers.Base, sprite.AddLayerState(_state));
            }

            sprite.LayerMapSet(PDAVisualLayers.Flashlight, sprite.AddLayerState("light_overlay"));
            sprite.LayerSetShader(PDAVisualLayers.Flashlight, "unshaded");
            sprite.LayerMapSet(PDAVisualLayers.IDLight, sprite.AddLayerState("id_overlay"));
            sprite.LayerSetShader(PDAVisualLayers.IDLight, "unshaded");

            var appearance = entityManager.GetComponent<PDAComponent>(entity);
            sprite.LayerSetVisible(PDAVisualLayers.IDLight, appearance.IdSlot.StartingItem != null);
        }

        [Obsolete("Subscribe to AppearanceChangeEvent instead.")]
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);
            var sprite = IoCManager.Resolve<IEntityManager>().GetComponent<ISpriteComponent>(component.Owner);
            sprite.LayerSetVisible(PDAVisualLayers.Flashlight, false);
            if (component.TryGetData(UnpoweredFlashlightVisuals.LightOn, out bool isFlashlightOn))
            {
                sprite.LayerSetVisible(PDAVisualLayers.Flashlight, isFlashlightOn);
            }
            if (component.TryGetData(PDAVisuals.IDCardInserted, out bool isCardInserted))
            {
                sprite.LayerSetVisible(PDAVisualLayers.IDLight, isCardInserted);
            }
        }
    }
}
