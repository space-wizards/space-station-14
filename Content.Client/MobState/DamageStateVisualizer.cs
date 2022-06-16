using Content.Shared.MobState;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client.MobState
{
    [UsedImplicitly]
    public sealed class DamageStateVisualizer : AppearanceVisualizer
    {
        private int? _originalDrawDepth;

        [DataField("states")]
        private Dictionary<DamageState, Dictionary<string, string>> _states = new();

        /// <summary>
        /// Should noRot be turned off when crit / dead.
        /// </summary>
        [DataField("rotate")]
        private bool _rotate;

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);
            var sprite = IoCManager.Resolve<IEntityManager>().GetComponent<ISpriteComponent>(component.Owner);
            if (!component.TryGetData(DamageStateVisuals.State, out DamageState data))
            {
                return;
            }

            if (!_states.TryGetValue(data, out var layers))
            {
                return;
            }

            if (_rotate)
            {
                sprite.NoRotation = data switch
                {
                    DamageState.Critical => false,
                    DamageState.Dead => false,
                    _ => true
                };
            }

            foreach (var (key, state) in layers)
            {
                sprite.LayerSetState(key, state);
            }

            // So they don't draw over mobs anymore
            if (data == DamageState.Dead && sprite.DrawDepth > (int) DrawDepth.Items)
            {
                _originalDrawDepth = sprite.DrawDepth;
                sprite.DrawDepth = (int) DrawDepth.Items;
            }
            else if (_originalDrawDepth != null)
            {
                sprite.DrawDepth = _originalDrawDepth.Value;
                _originalDrawDepth = null;
            }
        }
    }

    public enum DamageStateVisualLayers : byte
    {
        Base,
        BaseUnshaded,
    }
}
