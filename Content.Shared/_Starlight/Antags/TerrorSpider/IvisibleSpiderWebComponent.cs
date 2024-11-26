using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Magic;
using Robust.Shared.Serialization;

namespace Content.Shared._Starlight.Antags.TerrorSpider;
[RegisterComponent]
public sealed partial class StealthOnWebComponent : Component
{
    [DataField]
    public int Collisions = 0;
}
[RegisterComponent]
public sealed partial class EggHolderComponent : Component
{
    [DataField]
    public int Counter = 0;
}
[RegisterComponent]
public sealed partial class HasEggHolderComponent : Component
{
}
public sealed partial class EggInjectionEvent : EntityTargetActionEvent
{
}
[Serializable, NetSerializable]
public sealed partial class EggInjectionDoAfterEvent : SimpleDoAfterEvent
{
}
