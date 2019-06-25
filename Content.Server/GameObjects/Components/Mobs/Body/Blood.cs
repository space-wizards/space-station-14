namespace Content.Server.GameObjects.Components.Mobs.Body
{
    public class Blood
    {
        public float MaxVolume;
        public float CurrentVolume;
        public float RegenerationRate = 0.1f;
        public float BleedRate = 0f; //the value at which the blood spills out

        readonly int _levels = 7;

        public Blood(float volume)
        {
            MaxVolume = volume;
            CurrentVolume = MaxVolume;
        }

        public void ChangeCurrentVolume(float deltaVolume)
        {
            if (CurrentVolume + deltaVolume > MaxVolume)
            { 
                CurrentVolume = MaxVolume;
            }
            else if (CurrentVolume + deltaVolume < 0f)
            {
                CurrentVolume = 0f;
            }
            else
            {
                CurrentVolume += deltaVolume;
            }
        }

        public void Life()
        {
            regenerateBlood();
            loseBlood();
        }

        private void regenerateBlood() //TODO: Reagents will affect the blood regeneration rate, once we'll have reagents
        {
            if (CurrentVolume > 0)
            {
                ChangeCurrentVolume(RegenerationRate);
            }
        }

        private void loseBlood()
        {
            ChangeCurrentVolume(BleedRate);
            makeBloodSplatter();
        }

        public void makeBloodSplatter()
        {
            //TODO: make blood splatter effect
        }

        public BloodLevel GetBloodLevel()
        {
            var diff = (int)CurrentVolume / (MaxVolume / _levels);
            return (BloodLevel)diff;
        }

    }
    public enum BloodLevel
    {
        Max = 6,
        Safe = 5,
        Warn = 4,
        Okay = 3,
        Bad = 2,
        Survive = 1,
        None = 0
    }
}
