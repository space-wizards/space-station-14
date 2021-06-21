using Content.Shared.Smoking;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Smoking
{
    [UsedImplicitly]
    public class BurnStateVisualizer : AppearanceVisualizer
    {
        [DataField("burntIcon")]
        private string _burntIcon = "burnt-icon";
        [DataField("litIcon")]
        private string _litIcon = "lit-icon";
        [DataField("unlitIcon")]
        private string _unlitIcon = "icon";

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (component.TryGetData<SharedBurningStates>(SmokingVisuals.Smoking, out var smoking))
            {
                SetState(component, smoking);
            }
        }

        private void SetState(AppearanceComponent component, SharedBurningStates burnState)
        {
            if (component.Owner.TryGetComponent<ISpriteComponent>(out var sprite))
            {
                switch (burnState)
                {
                    case SharedBurningStates.Lit:
                        sprite.LayerSetState(0, _litIcon);
                        break;
                    case SharedBurningStates.Burnt:
                        sprite.LayerSetState(0, _burntIcon);
                        break;
                    default:
                        sprite.LayerSetState(0, _unlitIcon);
                        break;
                }
            }
        }
    }
}
