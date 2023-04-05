using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared.Disease
{
    /// <summary>
    /// Diseases encompass everything from viruses to cancers to heart disease.
    /// It's not just a virology thing.
    /// </summary>
    [Prototype("disease")]
    [DataDefinition]
    public sealed class DiseasePrototype : IPrototype, IInheritingPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; } = default!;

        [DataField("name")]
        public string Name { get; private set; } = string.Empty;

        [ParentDataFieldAttribute(typeof(AbstractPrototypeIdArraySerializer<DiseasePrototype>))]
        public string[]? Parents { get; private set; }

        [NeverPushInheritance]
        [AbstractDataFieldAttribute]
        public bool Abstract { get; private set; }

        /// <summary>
        /// Controls how often a disease ticks.
        /// </summary>
        [ViewVariables]
        public float TickTime = 1f;

        /// <summary>
        /// Since disease isn't mapped to metabolism or anything,
        /// it needs something to control its tickrate
        /// </summary>
        public float Accumulator = 0f;
        /// <summary>
        /// Since accumulator is reset with TickTime, this just tracks
        /// the total amount of time a disease has been present.
        /// </summary>
        public float TotalAccumulator = 0f;
        /// <summary>
        /// Stores all the separate stages of the disease plus the time
        /// thresholds for their activation
        /// int: the disease stage (0 for baseline, 1, 2, etc.)
        /// float: the time it takes for the stage to begin.
        /// </summary>
        [DataField("stages", serverOnly: true)]
        public readonly List<float> Stages = new() { 0f };
        /// <summary>
        /// List of effects the disease has that will
        /// run every second (by default anyway)
        /// </summary>
        [DataField("effects", serverOnly: true)]
        public readonly List<DiseaseEffect> Effects = new(0);
        /// <summary>
        /// List of SPECIFIC CURES the disease has that will
        /// be checked every second.
        /// Stuff like spaceacillin operates outside this.
        /// </summary>
        [DataField("cures", serverOnly: true)]
        public readonly List<DiseaseCure> Cures = new(0);
        /// <summary>
        /// This flatly reduces the probabilty disease medicine
        /// has to cure it every tick. Although, since spaceacillin is
        /// used as a reference and it has 0.15 chance, this is
        /// a base 33% reduction in cure chance
        /// </summary>
        [DataField("cureResist", serverOnly: true)]
        public float CureResist = 0.05f;
        /// <summary>
        /// Whether the disease can infect other people.
        /// Since this isn't just a virology thing, this
        /// primary determines what sort of disease it is.
        /// This also affects things like the vaccine machine.
        /// You can't print a cancer vaccine
        /// </summary>
        [DataField("infectious", serverOnly: true)]
        public bool Infectious = true;
    }
}
