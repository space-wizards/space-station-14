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
            blood = Owner.GetComponent<SpeciesComponent>().BodyTemplate.Blood;
            
        }

        public override void ApplyOrganData()
        {
            MaxRate = (int)OrganData["MaxRate"];
            CurrentRate = MaxRate / 2;
        }

        public override void Life()
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

        public PulseLevel GetPulse()
        {
            if (blood.GetBloodLevel() < BloodLevel.Bad)
            {
                return PulseLevel.Thready;
            }
            var diff = CurrentRate / (MaxRate / _levels);
            return (PulseLevel)diff;
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
