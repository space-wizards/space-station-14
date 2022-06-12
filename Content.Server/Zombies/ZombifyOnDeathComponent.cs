using Content.Shared.Weapons.Melee;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Zombies
{
    [RegisterComponent]
    public sealed class ZombifyOnDeathComponent : Component
    {
        [DataField("skinColor")]
        public Color SkinColor = new Color(0.70f, 0.72f, 0.48f, 1);

        [DataField("attackArc", customTypeSerializer: typeof(PrototypeIdSerializer<MeleeWeaponAnimationPrototype>))]
        public string AttackArc = "claw";
    }
}
