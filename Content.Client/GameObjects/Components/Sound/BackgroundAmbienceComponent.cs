using System.Collections.Generic;
using System.Threading;
using Content.Shared.GameObjects.Components.Sound;
using Content.Shared.Physics;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Client.GameObjects.Components.Sound
{
    [RegisterComponent]
    public class BackgroundAmbienceComponent : Component
    {
        public override string Name => "BackgroundAmbience";

        [ViewVariables(VVAccess.ReadWrite)]
        private bool debugDoStart = false;

        private bool started = false;

        private readonly CancellationTokenSource _timerCancelTokenSource = new CancellationTokenSource();

        private LoopingSoundComponent _soundComponent;

        private ScheduledSound _scheduledSound = new ScheduledSound();


        private AudioParams _audioParams = new AudioParams();

        protected override void Startup()
        {
            _scheduledSound.Filename = "/Audio/machines/microwave_loop.ogg";
            _scheduledSound.AudioParams = AudioParams.Default;
            _scheduledSound.Times = 4;
            _scheduledSound.Delay = 50;

            base.Startup();
            _soundComponent = Owner.GetComponent<LoopingSoundComponent>();

            Timer.SpawnRepeating(500, CheckConditions, _timerCancelTokenSource.Token);

        }

        private void CheckConditions()
        {
            //Circle _area = Owner.GetComponent<TransformComponent>()
            //Tile[] _tiles;

            //for(Owner.)

            if (debugDoStart && !started)
            {
                started = true;
                _soundComponent.AddScheduledSound(_scheduledSound);
                Timer.Spawn(500, () =>
                {
                    _soundComponent.FadeStopScheduledSound(_scheduledSound.Filename, 500);
                    //_soundComponent.StopScheduledSound(_scheduledSound.Filename);
                });
            }
        }
    }
}
