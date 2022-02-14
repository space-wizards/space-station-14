using Content.Server.Atmos;
using Content.Server.Body.Systems;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;
using Robust.Shared.ViewVariables;

namespace Content.Server.Body.Components
{
    [RegisterComponent, Friend(typeof(BloodstreamSystem))]
    public sealed class BloodstreamComponent : Component
    {
        public static string DefaultChemicalsSolutionName = "chemicals";
        public static string DefaultBloodSolutionName = "bloodstream";

        public float AccumulatedFrametime = 0.0f;

        /// <summary>
        ///     How much is this entity currently bleeding?
        ///     Higher numbers mean more bloodloss.
        ///
        ///     Goes down slowly over time, and items like bandages
        ///     or clotting reagents can lower bleeding.
        /// </summary>
        /// <remarks>
        ///     This generally corresponds to an amount of damage (slashing/piercing)
        /// </remarks>
        public float BleedAmount;

        /// <summary>
        ///     How frequently should this bloodstream update, in seconds?
        /// </summary>
        [DataField("updateInterval")]
        public float UpdateInterval = 5.0f;

        /// <summary>
        ///     A modifier set prototype ID corresponding to how damage should be modified
        ///     before taking it into account for bloodloss.
        /// </summary>
        /// <remarks>
        ///     For example, piercing damage is increased while poison damage is nullified entirely.
        /// </remarks>
        [DataField("damageBleedModifiers", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<DamageModifierSetPrototype>))]
        public string DamageBleedModifiers = "BloodlossHuman";

        // TODO probably damage bleed thresholds.

        /// <summary>
        ///     Max volume of internal chemical solution storage
        /// </summary>
        [DataField("chemicalMaxVolume")]
        public FixedPoint2 ChemicalMaxVolume = FixedPoint2.New(250);

        /// <summary>
        ///     Max volume of internal blood storage
        /// </summary>
        [DataField("bloodMaxVolume")]
        public FixedPoint2 BloodMaxVolume = FixedPoint2.New(100);

        /// <summary>
        ///     Which reagent is considered this entities 'blood'?
        /// </summary>
        /// <remarks>
        ///     Slimepeople might use slime as their blood or something like that.
        /// </remarks>
        [DataField("bloodReagent")]
        public string BloodReagent = "Blood";

        /// <summary>
        ///     Internal solution for reagent storage
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public Solution ChemicalSolution = default!;

        /// <summary>
        ///     Internal solution for blood storage
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public Solution BloodSolution = default!;
    }
}
