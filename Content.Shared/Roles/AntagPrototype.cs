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
        [IdDataFieldAttribute]
        public string ID { get; } = default!;

        /// <summary>
        ///     The name of this antag as displayed to players.
        /// </summary>
        [DataField("name")]
        private string _name { get; } = string.Empty;

        [ViewVariables(VVAccess.ReadOnly)]
        public string Name
        {
            get => Loc.GetString(_name);
        }

        /// <summary>
        ///     The antag's objective, displayed at round-start to the player.
        /// </summary>
        [DataField("objective")]
        private string _objective { get; } = string.Empty;

        [ViewVariables(VVAccess.ReadOnly)]
        public string Objective
        {
            get => Loc.GetString(_objective);
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
