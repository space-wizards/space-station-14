using Content.Shared.Nutrition.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
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

        [Obsolete("Subscribe to AppearanceChangeEvent instead.")]
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var entities = IoCManager.Resolve<IEntityManager>();
            if (!entities.TryGetComponent(component.Owner, out ISpriteComponent? sprite))
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
