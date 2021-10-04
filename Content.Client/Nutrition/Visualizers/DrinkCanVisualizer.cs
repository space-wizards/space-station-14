using Content.Shared.Nutrition.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Nutrition.Visualizers
{
    [UsedImplicitly]
    public sealed class DrinkCanVisualizer : AppearanceVisualizer
    {
        [DataField("stateClosed")]
        private string? _stateClosed;

        [DataField("stateOpen")]
        private string? _stateOpen;

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent<ISpriteComponent>(out var sprite))
            {
                return;
            }

            if (component.TryGetData<bool>(DrinkCanStateVisual.Opened, out var opened) && opened)
            {
                sprite.LayerSetState(DrinkCanVisualLayers.Icon, $"{_stateOpen}");
                return;
            }

            sprite.LayerSetState(DrinkCanVisualLayers.Icon, $"{_stateClosed}");
        }
    }

    public enum DrinkCanVisualLayers : byte
    {
        Icon = 0
    }
}
