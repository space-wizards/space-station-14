using Content.Shared.GameObjects;
using System.Collections.Generic;

namespace Content.Client.GameObjects.Components
{
    public class DamageableComponent : SharedDamageableComponent
    {
        /// <inheritdoc />
        public override string Name => "Damageable";

        public Dictionary<DamageType, int> CurrentDamage = new Dictionary<DamageType, int>();
    }
}
