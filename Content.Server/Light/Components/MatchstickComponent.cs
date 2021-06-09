#nullable enable
using System.Threading.Tasks;
using Content.Shared.Audio;
using Content.Shared.Interaction;
using Content.Shared.Smoking;
using Content.Shared.Temperature;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Light.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IHotItem))]
    public class MatchstickComponent : Component, IHotItem, IInteractUsing
    {
        public override string Name => "Matchstick";

        private SharedBurningStates _currentState = SharedBurningStates.Unlit;

        /// <summary>
        /// How long will matchstick last in seconds.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)] [DataField("duration")]
        private int _duration = 10;

        /// <summary>
        /// Sound played when you ignite the matchstick.
        /// </summary>
        [DataField("igniteSound")] private string? _igniteSound;

        /// <summary>
        /// Point light component. Gives matches a glow in dark effect.
        /// </summary>
        [ComponentDependency]
        private readonly PointLightComponent? _pointLightComponent = default!;

        /// <summary>
        /// Current state to matchstick. Can be <code>Unlit</code>, <code>Lit</code> or <code>Burnt</code>.
        /// </summary>
        [ViewVariables]
        public SharedBurningStates CurrentState
        {
            get => _currentState;
            private set
            {
                _currentState = value;

                if (_pointLightComponent != null)
                {
                    _pointLightComponent.Enabled = _currentState == SharedBurningStates.Lit;
                }

                if (Owner.TryGetComponent(out AppearanceComponent? appearance))
                {
                    appearance.SetData(SmokingVisuals.Smoking, _currentState);
                }
            }
        }

        bool IHotItem.IsCurrentlyHot()
        {
            return CurrentState == SharedBurningStates.Lit;
        }

        public void Ignite(IEntity user)
        {
            // Play Sound
            if (!string.IsNullOrEmpty(_igniteSound))
            {
                SoundSystem.Play(Filter.Pvs(Owner), _igniteSound, Owner,
                    AudioHelpers.WithVariation(0.125f).WithVolume(-0.125f));
            }

            // Change state
            CurrentState = SharedBurningStates.Lit;
            Owner.SpawnTimer(_duration * 1000, () => CurrentState = SharedBurningStates.Burnt);
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (eventArgs.Target.TryGetComponent<IHotItem>(out var hotItem)
                && hotItem.IsCurrentlyHot()
                && CurrentState == SharedBurningStates.Unlit)
            {
                Ignite(eventArgs.User);
                return true;
            }

            return false;
        }
    }
}
