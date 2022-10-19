using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Roles
{
    /// <summary>
    ///     Describes information for a single antag.
    /// </summary>
    [Prototype("antag")]
    public readonly record struct AntagPrototype : IPrototype
    {
        private readonly string _name = string.Empty;
        private readonly string _objective = string.Empty;

        [ViewVariables]
        [IdDataFieldAttribute]
        public string ID { get; } = default!;

        /// <summary>
        ///     The name of this antag as displayed to players.
        /// </summary>
        [DataField("name", customTypeSerializer: typeof(LocStringSerializer))]
        public readonly string Name = string.Empty;

        /// <summary>
        ///     The antag's objective, displayed at round-start to the player.
        /// </summary>
        [DataField("objective", customTypeSerializer: typeof(LocStringSerializer))]
        public readonly string Objective = string.Empty;

        /// <summary>
        ///     Whether or not the antag role is one of the bad guys.
        /// </summary>
        [DataField("antagonist")]
        public bool Antagonist { get; }

        /// <summary>
        ///     Whether or not the player can set the antag role in antag preferences.
        /// </summary>
        [DataField("setPreference")]
        public bool SetPreference { get; }
    }
}
