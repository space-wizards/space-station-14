using Content.Shared.Smoking;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Smoking
{
    [UsedImplicitly]
    public sealed class BurnStateVisualizer : AppearanceVisualizer, ISerializationHooks
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        [DataField("burntIcon")]
        private string _burntIcon = "burnt-icon";
        [DataField("litIcon")]
        private string _litIcon = "lit-icon";
        [DataField("unlitIcon")]
        private string _unlitIcon = "icon";

        void ISerializationHooks.AfterDeserialization()
        {
            IoCManager.InjectDependencies(this);
        }

        [Obsolete("Subscribe to AppearanceChangeEvent instead.")]
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!_entMan.TryGetComponent(component.Owner, out SpriteComponent? sprite))
                return;

            if (!component.TryGetData<SmokableState>(SmokingVisuals.Smoking, out var burnState))
                return;

            var state = burnState switch
            {
                SmokableState.Lit => _litIcon,
                SmokableState.Burnt => _burntIcon,
                _ => _unlitIcon
            };

            sprite.LayerSetState(0, state);
        }
    }
}
