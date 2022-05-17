using Content.Shared.Damage;
using Content.Shared.Sound;

namespace Content.Server.Weapon.Melee.EnergySword
{
    [RegisterComponent]
    internal sealed class EnergySwordComponent : Component
    {
        public Color BladeColor = Color.DodgerBlue;

        public bool Hacked = false;

        public bool Activated = false;

        /// <summary>
        ///     RGB cycle rate for hacked e-swords.
        /// </summary>
        [DataField("cycleRate")]
        public float CycleRate = 1f;

        [DataField("activateSound")]
        public SoundSpecifier ActivateSound { get; set; } = new SoundPathSpecifier("/Audio/Weapons/ebladeon.ogg");

        [DataField("deActivateSound")]
        public SoundSpecifier DeActivateSound { get; set; } = new SoundPathSpecifier("/Audio/Weapons/ebladeoff.ogg");

        [DataField("colorOptions")]
        public List<Color> ColorOptions = new()
        {
            Color.Tomato,
            Color.DodgerBlue,
            Color.Aqua,
            Color.MediumSpringGreen,
            Color.MediumOrchid
        };

        [DataField("litDamageBonus", required: true)]
        public DamageSpecifier LitDamageBonus = default!;
    }
}
