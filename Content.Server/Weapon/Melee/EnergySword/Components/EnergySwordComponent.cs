using System.Collections.Generic;
using Content.Shared.Damage;
using Content.Shared.Sound;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Weapon.Melee.EnergySword
{
    [RegisterComponent]
    internal class EnergySwordComponent : Component
    {
        public Color BladeColor = Color.DodgerBlue;

        public bool Hacked = false;

        public bool Activated = false;
        [DataField("hitSound")]
        public SoundSpecifier HitSound { get; set; } = new SoundPathSpecifier("/Audio/Weapons/eblade1.ogg");

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
