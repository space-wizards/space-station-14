using Content.Shared.Damage;
using Robust.Shared.Physics.Collision.Shapes;

namespace Content.Server.Atmos.Components
{
    [RegisterComponent]
    public sealed class FlammableComponent : Component
    {
        [ViewVariables]
        public bool Resisting = false;

        [ViewVariables]
        public readonly List<EntityUid> Collided = new();

        [ViewVariables(VVAccess.ReadWrite)]
        public bool OnFire { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public float FireStacks { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("fireSpread")]
        public bool FireSpread { get; private set; } = false;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("canResistFire")]
        public bool CanResistFire { get; private set; } = false;

        [DataField("damage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = new(); // Empty by default, we don't want any funny NREs.

        /// <summary>
        ///     Used for the fixture created to handle passing firestacks when two flammable objects collide.
        /// </summary>
        [DataField("flammableCollisionShape")]
        public IPhysShape FlammableCollisionShape = new PhysShapeCircle(0.35f);
    }
}
