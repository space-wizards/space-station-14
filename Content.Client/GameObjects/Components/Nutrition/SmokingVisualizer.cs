using Content.Shared.GameObjects.Components.Nutrition;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;

namespace Content.Client.GameObjects.Components.Nutrition
{
    [UsedImplicitly]
    public class SmokingVisualizer : AppearanceVisualizer
    {

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (component.TryGetData<SmokingStates>(SmokingVisuals.Smoking, out var smoking))
            {
                SetSmoking(component, smoking);
            }
        }

        private void SetSmoking(AppearanceComponent component, SmokingStates smoking)
        {
            var sprite = component.Owner.GetComponent<ISpriteComponent>();

            switch (smoking)
            {
                case SmokingStates.Lit:
                    sprite.LayerSetState(0, "lit-icon");
                    break;
                case SmokingStates.Burnt:
                    sprite.LayerSetState(0, "burnt-icon");
                    break;
                default:
                    sprite.LayerSetState(0, "icon");
                    break;
            }
        }
    }

}
