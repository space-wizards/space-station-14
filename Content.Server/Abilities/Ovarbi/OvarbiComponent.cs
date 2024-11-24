using Content.Shared.Damage;

/* New Frontiers - Ovarbi Changes - modifying the .yml to not be specific to Oni.
This code is licensed under AGPLv3. See AGPLv3.txt */
namespace Content.Server.Abilities.Ovarbi
{
    [RegisterComponent]
    public sealed partial class OvarbiComponent : Component
    {
        [DataField("modifiers", required: true)]
        public DamageModifierSet MeleeModifiers = default!;

        [DataField("stamDamageBonus")]
        public float StamDamageMultiplier = 1.20f;
    }
}
// End of modified code
