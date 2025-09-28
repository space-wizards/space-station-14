using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Inventory;
using Content.Shared.Starlight.Medical.Surgery;

namespace Content.Shared._Starlight.Access;
[ByRefEvent]
public record struct AccessReadEvent() 
{
    public bool Denied = false;
}
