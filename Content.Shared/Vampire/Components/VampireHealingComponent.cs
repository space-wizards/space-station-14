using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Damage;

namespace Content.Shared.Vampire.Components
{
    [RegisterComponent]
    public sealed partial class VampireHealingComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;

        public double NextHealTick = 0;

        public TimeSpan HealTickInterval = TimeSpan.FromSeconds(1);
    }
}
