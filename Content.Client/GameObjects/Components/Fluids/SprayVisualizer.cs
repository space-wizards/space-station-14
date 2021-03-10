using Content.Shared.GameObjects.Components.Fluids;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Fluids
{
    [UsedImplicitly]
    public class SprayVisualizer : AppearanceVisualizer
    {
        [DataField("safety_on_state")]
        private string _safetyOnState;
        [DataField("safety_off_state")]
        private string _safetyOffState;

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (component.TryGetData<bool>(SprayVisuals.Safety, out var safety))
            {
                SetSafety(component, safety);
            }
        }

        private void SetSafety(AppearanceComponent component, bool safety)
        {
            var sprite = component.Owner.GetComponent<ISpriteComponent>();

            sprite.LayerSetState(SprayVisualLayers.Base, safety ? _safetyOnState : _safetyOffState);
        }
    }

    public enum SprayVisualLayers : byte
    {
        Base
    }
}
