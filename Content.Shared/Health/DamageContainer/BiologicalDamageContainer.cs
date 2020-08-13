using System;
using System.Collections.Generic;
using Robust.Shared.Serialization;

namespace Content.Shared.Health.DamageContainer
{
    [NetSerializable, Serializable]
    public class BiologicalDamageContainer : AbstractDamageContainer
    {
        public override List<DamageClass> SupportedDamageClasses {
            get { return new List<DamageClass> { DamageClass.Brute, DamageClass.Burn, DamageClass.Toxin, DamageClass.Airloss }; }
        }
    }
}

