using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared.Kitchen
{

     [Serializable, NetSerializable]
     public enum MicrowaveVisualState
     {
         Off,
         PoweredIdle,
         Cooking
     }

    
}
