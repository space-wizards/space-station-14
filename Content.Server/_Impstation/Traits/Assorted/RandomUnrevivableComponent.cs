using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server._Impstation.Traits.Assorted;

[RegisterComponent]
public sealed partial class RandomUnrevivableComponent : Component
{
    [DataField("chance")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float Chance = 0.5f;
}
