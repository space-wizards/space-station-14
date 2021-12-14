using Content.Shared.Morgue;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Morgue.Visualizers
{
    [UsedImplicitly]
    public sealed class CrematoriumVisualizer : AppearanceVisualizer
    {
        [DataField("state_open")]
        private string _stateOpen = "";
        [DataField("state_closed")]
        private string _stateClosed = "";

        [DataField("light_contents")]
        private string _lightContents = "";
        [DataField("light_burning")]
        private string _lightBurning = "";

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var entities = IoCManager.Resolve<IEntityManager>();
            if (!entities.TryGetComponent(component.Owner, out ISpriteComponent? sprite))
            {
                return;
            }

            if (component.TryGetData(MorgueVisuals.Open, out bool open))
            {
                sprite.LayerSetState(CrematoriumVisualLayers.Base, open ? _stateOpen : _stateClosed);
            }
            else
            {
                sprite.LayerSetState(CrematoriumVisualLayers.Base, _stateClosed);
            }

            var lightState = "";
            if (component.TryGetData(MorgueVisuals.HasContents,  out bool hasContents) && hasContents) lightState = _lightContents;
            if (component.TryGetData(CrematoriumVisuals.Burning, out bool isBurning)   && isBurning)   lightState = _lightBurning;

            if (!string.IsNullOrEmpty(lightState))
            {
                sprite.LayerSetState(CrematoriumVisualLayers.Light, lightState);
                sprite.LayerSetVisible(CrematoriumVisualLayers.Light, true);
            }
            else
            {
                sprite.LayerSetVisible(CrematoriumVisualLayers.Light, false);
            }
        }
    }

    public enum CrematoriumVisualLayers : byte
    {
        Base,
        Light,
    }
}
