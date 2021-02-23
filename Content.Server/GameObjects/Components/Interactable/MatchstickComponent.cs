#nullable enable
using System.Threading.Tasks;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Interactable
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
        [ViewVariables(VVAccess.ReadOnly)] private int _duration;

        /// <summary>
        /// Sound played when you ignite the matchstick.
        /// </summary>
        private string? _igniteSound;

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

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _duration, "duration", 10);
            serializer.DataField(ref _igniteSound, "igniteSound", null);
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
                EntitySystem.Get<AudioSystem>().PlayFromEntity(_igniteSound, Owner,
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
