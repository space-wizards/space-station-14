using System;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components
{
    [RegisterComponent]
    public sealed class StandingStateComponent : Component
    {
        public override string Name => "StandingState";

        public override uint? NetID => ContentNetIDs.STANDING_STATE;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("downSoundCollection")]
        public string? DownSoundCollection { get; } = "BodyFall";

        [ViewVariables]
        [DataField("standing")]
        public bool Standing
        {
            get => _standing;
            set
            {
                if (_standing == value) return;

                _standing = value;
                if (_standing)
                {
                    Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, new StandEvent());
                }
                else
                {
                    Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, new DownEvent());
                }

                Dirty();
            }
        }

        private bool _standing = true;

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new StandingComponentState(_standing);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);
            if (curState is not StandingComponentState state) return;

            Standing = state.Standing;
        }

        // I'm not calling it StandingStateComponentState
        [Serializable, NetSerializable]
        private sealed class StandingComponentState : ComponentState
        {
            public bool Standing { get; }

            public StandingComponentState(bool standing) : base(ContentNetIDs.STANDING_STATE)
            {
                Standing = standing;
            }
        }
    }
}
