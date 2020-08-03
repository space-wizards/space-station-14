#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Content.Client.GameObjects.Components.Sound;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Sound;
using Content.Shared.GameObjects.EntitySystemMessages;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Components.Map;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Players;
using Robust.Shared.ViewVariables;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Client.GameObjects.Components
{
    [RegisterComponent]
    public class SingularityComponent : Component
    {
        public override uint? NetID => ContentNetIDs.SINGULARITY;
        public override string Name => "Singularity";

        private AudioSystem _audioSystem;
        private IPlayingAudioStream _playingStream;

        public override void Initialize()
        {
            base.Initialize();

            _audioSystem = EntitySystem.Get<AudioSystem>();
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession? session = null)
        {
            base.HandleNetworkMessage(message, netChannel, session);

            switch (message)
            {
                case SingularitySoundMessage msg:
                    if (msg.Start)
                    {
                        StartLoop();
                    }
                    else
                    {
                        StopLoop();
                    }

                    break;
            }
        }

        public void StartLoop()
        {
            AudioParams loopParams = new AudioParams();

            loopParams = AudioParams.Default;
            loopParams.Loop = true;

            Timer.Spawn(5200,
                () => _playingStream = _audioSystem.Play("/Audio/Effects/singularity.ogg", Owner, loopParams));

            _audioSystem.Play("/Audio/Effects/singularity_form.ogg", Owner);

        }

        public void StopLoop()
        {
            //_playingStream?.Stop();

            _audioSystem.Play("/Audio/Effects/singularity_collapse.ogg", Owner);
        }
    }
}
