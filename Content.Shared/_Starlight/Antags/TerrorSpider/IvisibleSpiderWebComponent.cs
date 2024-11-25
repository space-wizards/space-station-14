using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Actions;
using Content.Shared.Magic;

namespace Content.Shared._Starlight.Antags.TerrorSpider;
[RegisterComponent]
public sealed partial class StealthOnWebComponent : Component
{
    [DataField]
    public int Collisions = 0;
}
public sealed partial class EggInjectionEvent : EntityTargetActionEvent
{
}
