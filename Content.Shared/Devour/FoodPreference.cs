using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared.Devour
{
    [Serializable, NetSerializable]
    public enum FoodPreference : byte
    {
        Humanoid = 0,
        All = 1
    }
}
