using Content.Shared.Alert;
using Content.Shared.Standing;

namespace Content.Shared.MobState.State
{
    /// <summary>
    ///     A state in which an entity is disabled from acting due to sufficient damage (considered unconscious).
    /// </summary>
    public abstract class SharedCriticalMobState : BaseMobState
    {
        protected override DamageState DamageState => DamageState.Critical;
    }
}
