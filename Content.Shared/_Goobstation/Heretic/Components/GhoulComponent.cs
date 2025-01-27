using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared.Heretic;

[RegisterComponent, NetworkedComponent]
public sealed partial class GhoulComponent : Component
{
    /// <summary>
    ///     Indicates who ghouled the entity.
    /// </summary>
    [DataField] public EntityUid? BoundHeretic = null;

    /// <summary>
    ///     Total health for ghouls.
    /// </summary>
    [DataField] public FixedPoint2 TotalHealth = 50;
}
