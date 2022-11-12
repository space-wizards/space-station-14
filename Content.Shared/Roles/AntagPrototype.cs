using Robust.Shared.Prototypes;

namespace Content.Shared.Roles
{
    /// <summary>
    ///     Describes information for a single antag.
    /// </summary>
    [Prototype("antag")]
    public sealed class AntagPrototype : IPrototype
    {
        private string _name = string.Empty;
        private string _objective = string.Empty;
        private string? _description = string.Empty;

        [ViewVariables]
        [IdDataFieldAttribute]
        public string ID { get; } = default!;

        /// <summary>
        ///     The name of this antag as displayed to players.
        /// </summary>
        [DataField("name")]
        public string Name
        {
            get => _name;
            private set => _name = Loc.GetString(value);
        }

        /// <summary>
        ///     The description of this antag shown in a tooltip.
        /// </summary>
        [DataField("description")]
        public string? Description
        {
            get => _description;
            private set => _description = value is null ? null : Loc.GetString(value);
        }

        /// <summary>
        ///     The antag's objective, displayed at round-start to the player.
        /// </summary>
        [DataField("objective")]
        public string Objective
        {
            get => _objective;
            private set => _objective = Loc.GetString(value);
        }

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
