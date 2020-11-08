#nullable enable
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Mobs.State;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Mobs.State
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedMobStateComponent))]
    public class MobStateComponent : SharedMobStateComponent
    {
        private readonly Dictionary<DamageState, IMobState> _behavior = new Dictionary<DamageState, IMobState>
        {
            {DamageState.Alive, new NormalState()},
            {DamageState.Critical, new CriticalState()},
            {DamageState.Dead, new DeadState()}
        };

        protected override IReadOnlyDictionary<DamageState, IMobState> Behavior => _behavior;
    }
}
