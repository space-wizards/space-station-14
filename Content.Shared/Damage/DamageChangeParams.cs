#nullable enable
using System;
using Content.Shared.Body.Components;
using Content.Shared.Damage.Components;

namespace Content.Shared.Damage
{
    /// <summary>
    ///     Data class with information on how to damage a
    ///     <see cref="IDamageableComponent"/>.
    ///     While not necessary to damage for all instances, classes such as
    ///     <see cref="SharedBodyComponent"/> may require it for extra data
    ///     (such as selecting which limb to target).
    /// </summary>
    public class DamageChangeParams : EventArgs
    {
    }
}
