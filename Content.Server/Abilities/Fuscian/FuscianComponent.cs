using Content.Shared.Damage;

/* New Frontiers - Fuscian Changes - modifying the .yml to not be specific to Oni.
This code is licensed under AGPLv3. See AGPLv3.txt */
namespace Content.Server.Abilities.Fuscian
{
    [RegisterComponent]
    public sealed partial class FuscianComponent : Component
    {
        [DataField("modifiers", required: true)]
        public DamageModifierSet MeleeModifiers = default!;

        [DataField("stamDamageBonus")]
        public float StamDamageMultiplier = 0.8f;
    }
}
// End of modified code
