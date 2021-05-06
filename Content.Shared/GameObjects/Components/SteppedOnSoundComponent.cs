using System;
using Content.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
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
        public SoundCollectionPrototype SoundCollection
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
        private SoundCollectionPrototype _soundCollection = default!;

        /// <summary>
        /// This is to prevent the sound being spammed every frame.
        /// </summary>
        /// <remarks>
        /// Pointless to sync this between client and server.
        /// </remarks>
        public TimeSpan LastStep { get; set; } = TimeSpan.Zero;

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new SteppedOnSoundComponentState(_soundCollection.ID);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);
            if (curState is not SteppedOnSoundComponentState stepOnMe) return;
            SoundCollection = IoCManager.Resolve<IPrototypeManager>().Index<SoundCollectionPrototype>(stepOnMe.Sound);
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
