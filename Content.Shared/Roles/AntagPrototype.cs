using Robust.Shared.Prototypes;

namespace Content.Shared.Roles
{
    /// <summary>
    ///     Describes information for a single antag.
    /// </summary>
    [Prototype("antag")]
    public sealed class AntagPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; private set; } = default!;

        /// <summary>
        ///     The name of this antag as displayed to players.
        /// </summary>
        [DataField("name")]
        public string Name { get; private set; } = "";

        /// <summary>
        ///     The description of this antag shown in a tooltip.
        /// </summary>
        [DataField("description")]
        public string? Description { get; private set; }

        /// <summary>
        ///     The antag's objective, displayed at round-start to the player.
        /// </summary>
        [DataField("objective")]
        public string Objective { get; private set; } = "";

        /// <summary>
        ///     Whether or not the antag role is one of the bad guys.
        /// </summary>
        [DataField("antagonist")]
        public bool Antagonist { get; private set; }

        /// <summary>
        ///     Whether or not the player can set the antag role in antag preferences.
        /// </summary>
        [DataField("setPreference")]
        public bool SetPreference { get; private set; }
    }
}
