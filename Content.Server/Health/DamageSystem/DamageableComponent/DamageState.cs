using Robust.Shared.Interfaces.GameObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Server.DamageSystem
{
    /// <summary>
    ///     Defines what state an <see cref="IEntity"/> with a <see cref="IDamageableComponent"/> is in. Not all states must be supported -
    ///     for instance, the <see cref="BasicDamageableComponent"/> only supports <see cref="DamageState.Alive"/> and <see cref="DamageState.Dead"/>,
    ///     as inanimate objects don't go into crit.
    /// </summary>
    public enum DamageState { Alive, Critical, Dead }
}
