using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components
{
    /// <summary>
    /// Plays a sound when a SteppedOnEvent is triggered.
    /// </summary>
    [RegisterComponent]
    public sealed class SteppedOnSoundComponent : Component
    {
        public override string Name => "SteppedOnSound";

        [ViewVariables(VVAccess.ReadWrite)]
        public string SoundCollection
        {
            get => _soundCollection;
            private set
            {
                if (_soundCollection.Equals(value)) return;
                _soundCollection = value;
                Dirty();
            }
        }

        [DataField("soundCollection")]
        private string _soundCollection = default!;

        /// <summary>
        /// This is to prevent the sound being spammed every frame.
        /// </summary>
        /// <remarks>
        /// Pointless to sync this between client and server.
        /// </remarks>
        public TimeSpan LastStep { get; set; } = TimeSpan.Zero;

        protected override void Startup()
        {
            base.Startup();
            Owner.EnsureComponentWarn<SteppedOnTriggerComponent>();
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new SteppedOnSoundComponentState(_soundCollection);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);
            if (curState is not SteppedOnSoundComponentState stepOnMe) return;
            SoundCollection = stepOnMe.Sound;
        }

        [Serializable, NetSerializable]
        private sealed class SteppedOnSoundComponentState : ComponentState
        {
            public readonly string Sound;

            public SteppedOnSoundComponentState(string sound) : base(ContentNetIDs.STEPPED_ON_SOUND)
            {
                Sound = sound;
            }
        }
    }
}
