using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Damage;

namespace Content.Shared.Vampire.Components
{
    [RegisterComponent]
    public sealed partial class CoffinComponent : Component
    {
        [DataField("damage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;
    }
}
