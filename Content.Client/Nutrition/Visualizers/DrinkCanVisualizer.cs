using Content.Client.Items.Components;
using Content.Shared.Chemistry.Solution.Components;
using Content.Shared.Nutrition.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Nutrition.Visualizers
{
    [UsedImplicitly]
    public sealed class DrinkCanVisualizer : AppearanceVisualizer
    {
        [DataField("state_closed")]
        private string? _stateClosed;

        [DataField("state_open")]
        private string? _stateOpen;

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var sprite = component.Owner.GetComponent<ISpriteComponent>();

            if (component.TryGetData<bool>(DrinkCanStateVisuals.Opened, out var opened) && opened)
            {
                sprite.LayerSetState(0, $"{_stateOpen}");
                return;
            }

            sprite.LayerSetState(0, $"{_stateClosed}");
        }
    }
}
