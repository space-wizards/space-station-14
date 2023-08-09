using Content.Shared.Nutrition.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Nutrition.Visualizers
{
    [UsedImplicitly]
    public sealed class CreamPiedVisualizer : AppearanceVisualizer
    {
        [DataField("state")]
        private string? _state;

        [Obsolete("Subscribe to your component being initialised instead.")]
        public override void InitializeEntity(EntityUid entity)
        {
            base.InitializeEntity(entity);

            var sprite = IoCManager.Resolve<IEntityManager>().GetComponent<ISpriteComponent>(entity);

            sprite.LayerMapReserveBlank(CreamPiedVisualLayers.Pie);
            sprite.LayerSetRSI(CreamPiedVisualLayers.Pie, "Effects/creampie.rsi");
            sprite.LayerSetVisible(CreamPiedVisualLayers.Pie, false);
        }

        [Obsolete("Subscribe to AppearanceChangeEvent instead.")]
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (component.TryGetData<bool>(CreamPiedVisuals.Creamed, out var pied))
            {
                SetPied(component, pied);
            }
        }

        private void SetPied(AppearanceComponent component, bool pied)
        {
            var sprite = IoCManager.Resolve<IEntityManager>().GetComponent<ISpriteComponent>(component.Owner);

            sprite.LayerSetVisible(CreamPiedVisualLayers.Pie, pied);
            sprite.LayerSetState(CreamPiedVisualLayers.Pie, _state);
        }
    }

    public enum CreamPiedVisualLayers : byte
    {
        Pie,
    }
}
