using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Interactable;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;

namespace Content.Client.GameObjects.Components.Interactable
{
    [UsedImplicitly]
    public class MatchstickVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (component.TryGetData<MatchstickState>(MatchstickVisual.Igniting, out var smoking))
            {
                SetIgnite(component, smoking);
            }
        }

        private void SetIgnite(AppearanceComponent component, MatchstickState smoking)
        {
            var sprite = component.Owner.GetComponent<ISpriteComponent>();

            switch (smoking)
            {
                case MatchstickState.Lit:
                    sprite.LayerSetState(0, "match_lit");
                    break;
                case MatchstickState.Burnt:
                    sprite.LayerSetState(0, "match_burnt");
                    break;
                default:
                    sprite.LayerSetState(0, "match_unlit");
                    break;
            }
        }
    }
}
