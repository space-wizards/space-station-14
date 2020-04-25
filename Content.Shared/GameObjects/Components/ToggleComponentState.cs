using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components
{
    [NetSerializable, Serializable]
    public class ToggleComponentState : ComponentState
    {     
        public int AmmountCapacity { get; }
        public float Ammount { get; }
        public float AmmountCost { get; }
        public float AmmountLossRate { get; }
        public bool Activated { get; }
        public string SoundOn { get; }
        public string SoundOff { get; }
        public string AmmountName { get; }
        public string AmmountColor1 { get; }
        public string AmmountColor2 { get; }

       public ToggleComponentState(int ammountCapacity, float ammount, float ammountCost, float ammountLossRate, bool activated, 
       string soundOn, string soundOff, string ammountName, string ammountColor1, string ammountColor2) : base(ContentNetIDs.TOGGLE)
        {
            AmmountCapacity = ammountCapacity;
            Ammount = ammount;
            AmmountCost = ammountCost;
            AmmountLossRate = ammountLossRate;
            Activated = activated;
            SoundOn = soundOn;
            SoundOff = soundOff;
            AmmountName = ammountName;
            AmmountColor1 = ammountColor1;
            AmmountColor2 = ammountColor2;
        }
    }
}
