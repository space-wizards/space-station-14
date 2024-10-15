using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.Atmos.Components
{
    [RegisterComponent]
    public sealed partial class FlammableComponent : Component
    {
        // An array which represents the atmos gases as mols, Specifies 1 mol of Oxygen.
        private static float[] _standardOxygenMols = new float[] { 1f }.Concat(Enumerable.Repeat(0f, Atmospherics.AdjustedNumberOfGases - 1)).ToArray();

        [DataField]
        public bool Resisting;

        /// <summary>
        /// A gas mix which specifies the minimum mols of each gas needed for the entity to burn. Set to null to burn without atmosphere.
        /// </summary>
        [DataField]
        public GasMixture? FuelGasMix = new GasMixture(_standardOxygenMols, Atmospherics.T20C, 1f);

        [DataField]
        public bool OnFire;

        [DataField]
        public float FireStacks;

        [DataField]
        public float MaximumFireStacks = 10f;

        [DataField]
        public float MinimumFireStacks = -10f;

        [DataField]
        public string FlammableFixtureID = "flammable";

        [DataField]
        public float MinIgnitionTemperature = 373.15f;

        /// <summary>
        /// The peak temperature the entity will reach by burning alone.
        /// </summary>
        [DataField]
        public float PeakFlameTemperature = 1500.00f; // vaguely how hot an incenerator gets.

        /// <summary>
        /// How much energy is released while the entity is burning. 
        /// </summary>
        [DataField]
        public float JoulesPerFirestack = 8500;

        [DataField]
        public bool FireSpread { get; private set; } = false;

        [DataField]
        public bool CanResistFire { get; private set; } = false;

        [DataField(required: true)]
        public DamageSpecifier Damage = new(); // Empty by default, we don't want any funny NREs.

        /// <summary>
        ///     Used for the fixture created to handle passing firestacks when two flammable objects collide.
        /// </summary>
        [DataField]
        public IPhysShape FlammableCollisionShape = new PhysShapeCircle(0.35f);

        /// <summary>
        ///     Should the component be set on fire by interactions with isHot entities
        /// </summary>
        [DataField]
        public bool AlwaysCombustible = false;

        /// <summary>
        ///     Can the component anyhow lose its FireStacks?
        /// </summary>
        [DataField]
        public bool CanExtinguish = true;

        /// <summary>
        ///     How many firestacks should be applied to component when being set on fire?
        /// </summary>
        [DataField]
        public float FirestacksOnIgnite = 2.0f;

        /// <summary>
        /// Determines how quickly the object will fade out. With positive values, the object will flare up instead of going out.
        /// </summary>
        [DataField]
        public float FirestackFade = -0.1f;

        [DataField]
        public ProtoId<AlertPrototype> FireAlert = "Fire";
    }
}
