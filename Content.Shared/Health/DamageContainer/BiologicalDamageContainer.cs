using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Content.Shared.BodySystem
{
    public class BiologicalDamageContainer : AbstractDamageContainer
    {
        private readonly List<DamageClass> SupportedDamageClasses = new List<DamageClass> { DamageClass.Brute, DamageClass.Burn, DamageClass.Toxin, DamageClass.Airloss };
    }
}

