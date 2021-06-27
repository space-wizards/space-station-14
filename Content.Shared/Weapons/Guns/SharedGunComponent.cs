using System;
using Content.Shared.NetIDs;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Weapons.Guns
{
    [RegisterComponent]
    public class SharedGunComponent : Component
    {
        public override string Name => "Gun";
        public override uint? NetID => ContentNetIDs.GUN;

        [ViewVariables]
        [DataField("soundGunshot")]
        public string? SoundGunshot { get; } = null;

        [ViewVariables]
        [DataField("fireRate")]
        public float FireRate { get; set; } = 0.0f;

        public bool Firing { get; set; }

        [ViewVariables]
        public TimeSpan NextFire
        {
            get => _nextFire;
            set
            {
                _nextFire = value;
                Dirty();
            }
        }

        private TimeSpan _nextFire;

        public float GetFireRate()
        {
            return FireRate > 0.0f ? 1 / FireRate : 0.0f;
        }

        protected override void Initialize()
        {
            base.Initialize();
            NextFire = TimeSpan.FromSeconds(IoCManager.Resolve<IGameTiming>().CurTime.TotalSeconds + 0.2);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);
            if (curState is not GunComponentState state) return;
            NextFire = state.NextFire;
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new GunComponentState(NextFire);
        }

        [Serializable, NetSerializable]
        protected sealed class GunComponentState : ComponentState
        {
            public TimeSpan NextFire { get; }

            public GunComponentState(TimeSpan nextFire) : base(ContentNetIDs.GUN)
            {
                NextFire = nextFire;
            }
        }
    }
}
