using Content.Shared.Extinguisher;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Extinguisher
{
    [UsedImplicitly]
    public sealed class FireExtinguisherVisualizer : AppearanceVisualizer
    {
        [DataField("safety_on_state")]
        private string? _safetyOnState;
        [DataField("safety_off_state")]
        private string? _safetyOffState;

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (component.TryGetData<bool>(FireExtinguisherVisuals.Safety, out var safety))
            {
                SetSafety(component, safety);
            }
        }

        private void SetSafety(AppearanceComponent component, bool safety)
        {
            var sprite = IoCManager.Resolve<IEntityManager>().GetComponent<ISpriteComponent>(component.Owner);

            sprite.LayerSetState(FireExtinguisherVisualLayers.Base, safety ? _safetyOnState : _safetyOffState);
        }
    }

    public enum FireExtinguisherVisualLayers : byte
    {
        Base
    }
}
