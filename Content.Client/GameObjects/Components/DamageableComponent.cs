using System.Collections.Generic;
using Content.Shared.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects
{
    /// <summary>
    /// Fuck I really hate doing this
    /// TODO: make sure the client only gets damageable component on the clientside entity for its player mob
    /// </summary>
    [RegisterComponent]
    public class DamageableComponent : SharedDamageableComponent
    {
        /// <inheritdoc />
        public override string Name => "Damageable";

        public Dictionary<DamageType, int> CurrentDamage = new Dictionary<DamageType, int>();

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            base.HandleComponentState(curState, nextState);

            if(curState is DamageComponentState damagestate)
            {
                CurrentDamage = damagestate.CurrentDamage;
            }
        }
    }
}
