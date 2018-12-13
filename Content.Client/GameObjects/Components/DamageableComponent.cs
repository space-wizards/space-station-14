using Content.Shared.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using System.Collections.Generic;

namespace Content.Client.GameObjects
{
    /// <summary>
    /// Fuck I really hate doing this
    /// TODO: make sure the client only gets damageable component on the clientside entity for its player mob
    /// </summary>
    public class DamageableComponent : SharedDamageableComponent
    {
        /// <inheritdoc />
        public override string Name => "Damageable";

        public Dictionary<DamageType, int> CurrentDamage = new Dictionary<DamageType, int>();

        public override void HandleComponentState(ComponentState state)
        {
            base.HandleComponentState(state);

            if(state is DamageComponentState)
            {
                var damagestate = (DamageComponentState)state;
                CurrentDamage = damagestate.CurrentDamage;
            }
        }
    }
}
