#nullable enable
using System;
using Content.Shared.GameObjects.Components.Body;

namespace Content.Shared.GameObjects.Components.Damage
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
