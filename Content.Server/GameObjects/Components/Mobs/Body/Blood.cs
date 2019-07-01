using Robust.Shared.Serialization;
using YamlDotNet.RepresentationModel;
using Robust.Shared.Prototypes;

namespace Content.Server.GameObjects.Components.Mobs.Body
{
    [Prototype("blood")]
    public class Blood : IPrototype, IIndexedPrototype
    {
        public string Name;
        string Id;
        float MaxVolume;

        public float CurrentVolume;
        public float RegenerationRate = 0.1f;
        /// <summary>
        /// Value at which the blood spills out.
        /// </summary>
        public float BleedRate = 0f;

        int _levels = 7;
        string IIndexedPrototype.ID => Id;

        void IPrototype.LoadFrom(YamlMappingNode mapping)
        {
            var obj = YamlObjectSerializer.NewReader(mapping);
            obj.DataField(ref Name, "name", "");
            obj.DataField(ref Id, "id", "");
            obj.DataField(ref MaxVolume, "maxVolume", 0);
            obj.DataField(ref CurrentVolume, "currentVolume", 0);
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

        /// <summary>
        /// Called on life tick, to make it bleed simple change BleedRate variable
        /// </summary>
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
