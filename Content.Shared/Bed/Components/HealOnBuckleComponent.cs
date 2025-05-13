using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.Bed.Components
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentPause, AutoGenerateComponentState]
    public sealed partial class HealOnBuckleComponent : Component
    {
        /// <summary>
        /// Damage to apply to entities that are strapped to this entity.
        /// </summary>
        [DataField(required: true)]
        public DamageSpecifier Damage = default!;

        /// <summary>
        /// How frequently the damage should be applied, in seconds.
        /// </summary>
        [DataField(required: false)]
        public float HealTime = 1f;

        /// <summary>
        /// Damage multiplier that gets applied if the entity is sleeping.
        /// </summary>
        [DataField]
        public float SleepMultiplier = 3f;

        [DataField, AutoPausedField, AutoNetworkedField]
        public TimeSpan NextHealTime = TimeSpan.Zero; //Next heal

        [DataField, AutoNetworkedField]
        public EntityUid? SleepAction;
    }
}
