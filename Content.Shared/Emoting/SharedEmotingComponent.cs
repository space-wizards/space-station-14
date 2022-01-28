using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Players;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Emoting
{
    [RegisterComponent, NetworkedComponent]
    public class SharedEmotingComponent : Component
    {
        [DataField("enabled")] private bool _enabled = true;
        public override string Name => "Emoting";

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value)
                    return;

                _enabled = value;
                Dirty();
            }
        }

        public override ComponentState GetComponentState()
        {
            return new EmotingComponentState(Enabled);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (curState is not EmotingComponentState emoting)
                return;

            _enabled = emoting.Enabled;
        }

        [Serializable, NetSerializable]
        private sealed class EmotingComponentState : ComponentState
        {
            public bool Enabled { get; }

            public EmotingComponentState(bool enabled)
            {
                Enabled = enabled;
            }
        }
    }
}
