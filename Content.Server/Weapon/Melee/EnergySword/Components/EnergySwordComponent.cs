using Content.Shared.Sound;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Maths;
using System.Collections.Generic;

namespace Content.Server.Weapon.Melee.Esword
{
    [RegisterComponent]
    internal class EnergySwordComponent : Component
    {
        public override string Name => "Esword";

        public Color BladeColor = Color.Blue;

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
    }
}
