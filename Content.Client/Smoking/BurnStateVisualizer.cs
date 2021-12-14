using Content.Client.Clothing;
using Content.Shared.Smoking;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
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

        [DataField("burntPrefix")]
        private string _burntPrefix = "unlit";
        [DataField("litPrefix")]
        private string _litPrefix = "lit";
        [DataField("unlitPrefix")]
        private string _unlitPrefix = "unlit";

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (component.TryGetData<SmokableState>(SmokingVisuals.Smoking, out var smoking))
            {
                SetState(component, smoking);
            }
        }

        private void SetState(AppearanceComponent component, SmokableState burnState)
        {
            var entities = IoCManager.Resolve<IEntityManager>();
            var clothing = entities.GetComponentOrNull<ClothingComponent>(component.Owner);

            if (entities.TryGetComponent(component.Owner, out ISpriteComponent sprite))
            {
                switch (burnState)
                {
                    case SmokableState.Lit:
                        if (clothing != null)
                            clothing.ClothingEquippedPrefix = _litPrefix;
                        sprite.LayerSetState(0, _litIcon);
                        break;
                    case SmokableState.Burnt:
                        if (clothing != null)
                            clothing.ClothingEquippedPrefix = _burntPrefix;
                        sprite.LayerSetState(0, _burntIcon);
                        break;
                    case SmokableState.Unlit:
                        if (clothing != null)
                            clothing.ClothingEquippedPrefix = _unlitPrefix;
                        sprite.LayerSetState(0, _unlitIcon);
                        break;
                }
            }
        }
    }
}
