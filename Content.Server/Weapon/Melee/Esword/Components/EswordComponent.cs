using Content.Shared.Sound;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Weapon.Melee.Esword
{
    [RegisterComponent]
    internal class EswordComponent : Component
    {
        public override string Name => "Esword";

        public bool Activated = true;

        [DataField("hitSound")]
        public SoundSpecifier HitSound { get; set; } = new SoundPathSpecifier("/Audio/Weapons/eblade1.ogg");

        [DataField("activateSound")]
        public SoundSpecifier ActivateSound { get; set; } = new SoundCollectionSpecifier("sparks");
    }
}
