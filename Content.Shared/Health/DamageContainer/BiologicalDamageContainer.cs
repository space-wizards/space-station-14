using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Content.Shared.BodySystem
{
    [NetSerializable, Serializable]
    public class BiologicalDamageContainer : AbstractDamageContainer
    {
        public override List<DamageClass> SupportedDamageClasses {
            get { return new List<DamageClass> { DamageClass.Brute, DamageClass.Burn, DamageClass.Toxin, DamageClass.Airloss }; }
        }
    }
}

