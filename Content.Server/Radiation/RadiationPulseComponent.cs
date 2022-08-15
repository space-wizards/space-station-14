using Content.Shared.Radiation;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Radiation
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedRadiationPulseComponent))]
    public sealed class RadiationPulseComponent : SharedRadiationPulseComponent
    {
        public void DoPulse()
        {
            if (Decay)
            {
                var currentTime = _gameTiming.CurTime;
                _startTime = currentTime;
                _duration = _random.NextFloat() * (MaxPulseLifespan - MinPulseLifespan) + MinPulseLifespan;
                _endTime = currentTime + TimeSpan.FromSeconds(_duration);
            }

            SoundSystem.Play(Sound.GetSound(), Filter.Pvs(Owner), Owner);

            Dirty();
        }

        public override ComponentState GetComponentState()
        {
            return new RadiationPulseState(_range, Draw, Decay, _startTime, _endTime);
        }
    }
}
