using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Damage;
using Content.Shared.Inventory;

namespace Content.Shared._Starlight.Medical.Damage;
public sealed class DamageBeforeApplyEvent : EntityEventArgs
{
    public required DamageSpecifier Damage;
    public EntityUid? Origin;

    public bool Cancelled { get; set; }
}
