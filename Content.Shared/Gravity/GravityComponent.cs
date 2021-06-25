using System;
using Content.Shared.NetIDs;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Gravity
{
    [RegisterComponent]
    public sealed class GravityComponent : Component
    {
        public override string Name => "Gravity";
        public override uint? NetID => ContentNetIDs.GRAVITY;

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

        public override ComponentState GetComponentState(ICommonSession player)
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
        protected sealed class GravityComponentState : ComponentState
        {
            public bool Enabled { get; }

            public GravityComponentState(bool enabled) : base(ContentNetIDs.GRAVITY)
            {
                Enabled = enabled;
            }
        }
    }
}
