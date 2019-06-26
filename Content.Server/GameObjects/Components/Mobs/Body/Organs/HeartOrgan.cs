using System;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components.Mobs.Body
{
    public class Heart : Organ
    {
        private int MaxRate; //PulseLevel.VeryFast level of pulse
        private int CurrentRate;
        readonly int _levels = 6;

        Blood blood;

        public override void Startup()
        {
            blood = Body.Blood;
            objPrototype = "HumanHeart";
        }

        public override void ApplyOrganData()
        {
            MaxRate = 240;//(int)OrganData["MaxRate"]; //TODO: YAML
            CurrentRate = MaxRate / 2;
        }

        public override void Life(int lifeTick)
        {
            switch (State)
            {
                case OrganState.Damaged:
                    blood.CurrentVolume *= 0.6f;
                    break;
                case OrganState.Dead:
                    blood.CurrentVolume = 0f;
                    break;
            }
        }

        public override void OnStateChange(OrganState oldState)
        {
            switch (State)
            {
                case OrganState.Healthy:
                    setRateLevel(PulseLevel.Normal);
                    break;
                case OrganState.Damaged:
                    setRateLevel(PulseLevel.Fast);
                    break;
                case OrganState.Dead:
                    setRateLevel(PulseLevel.None);
                    break;
            }
        }

        public PulseLevel GetPulse()
        {
            if ( (blood.GetBloodLevel() < BloodLevel.Bad) && (State != OrganState.Dead) )
            {
                return PulseLevel.Thready;
            }
            var diff = CurrentRate / (MaxRate / _levels);
            return (PulseLevel)diff;
        }

        private void setRateLevel(PulseLevel accordingToLevel)
        {
            if(accordingToLevel == PulseLevel.Thready)
            {
                throw new ArgumentException("Pulse level can't be set to Thready here");
            }
            setRate(MaxRate * ((int)accordingToLevel / _levels));
        }

        private void setRate(int rateValue)
        {
            //TODO: should be aware of reagents that affects the heartrate
            CurrentRate = rateValue;
        }
        
    }
    public enum PulseLevel
    {
        None = 0,
        VerySlow = 1,
        Slow = 2,
        Normal = 3,
        Fast = 4,
        VeryFast = 5,
        Thready
    }
}
