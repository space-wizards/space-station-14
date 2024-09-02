using Content.Shared.Alert;
using Content.Shared.Damage;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Prototypes;

namespace Content.Server.Atmos.Components
{
    [RegisterComponent]
    public sealed partial class FlammableComponent : Component
    {
        [DataField]
        public bool Resisting;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public bool OnFire;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public float FireStacks;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public float MaximumFireStacks = 10f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public float MinimumFireStacks = -10f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public string FlammableFixtureID = "flammable";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public float MinIgnitionTemperature = 373.15f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public bool FireSpread { get; private set; } = false;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public bool CanResistFire { get; private set; } = false;

        [DataField(required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = new(); // Empty by default, we don't want any funny NREs.

        /// <summary>
        ///     Used for the fixture created to handle passing firestacks when two flammable objects collide.
        /// </summary>
        [DataField]
        public IPhysShape FlammableCollisionShape = new PhysShapeCircle(0.35f);

        /// <summary>
        ///     Should the component be set on fire by interactions with isHot entities
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public bool AlwaysCombustible = false;

        /// <summary>
        ///     Can the component anyhow lose its FireStacks?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public bool CanExtinguish = true;

        /// <summary>
        ///     How many firestacks should be applied to component when being set on fire?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public float FirestacksOnIgnite = 2.0f;

        /// <summary>
        /// Determines how quickly the object will fade out. With positive values, the object will flare up instead of going out.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public float FirestackFade = -0.1f;

        [DataField]
        public ProtoId<AlertPrototype> FireAlert = "Fire";
    }
}
