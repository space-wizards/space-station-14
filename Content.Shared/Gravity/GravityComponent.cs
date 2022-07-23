using Content.Shared.Sound;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Gravity
{
    [RegisterComponent]
    [NetworkedComponent]
    public sealed class GravityComponent : Component
    {
        [DataField("gravityShakeSound")]
        public SoundSpecifier GravityShakeSound { get; set; } = new SoundPathSpecifier("/Audio/Effects/alert.ogg");

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value) return;
                _enabled = value;
                if (_enabled)
                {
                    Logger.Info($"Enabled gravity for {Owner}");
                }
                else
                {
                    Logger.Info($"Disabled gravity for {Owner}");
                }
                Dirty();
            }
        }

        private bool _enabled;

        public override ComponentState GetComponentState()
        {
            return new GravityComponentState(_enabled);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);
            if (curState is not GravityComponentState state) return;
            Enabled = state.Enabled;
        }

        [Serializable, NetSerializable]
        private sealed class GravityComponentState : ComponentState
        {
            public bool Enabled { get; }

            public GravityComponentState(bool enabled)
            {
                Enabled = enabled;
            }
        }
    }
}
