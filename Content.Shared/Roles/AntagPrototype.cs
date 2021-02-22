using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Roles
{
    /// <summary>
    ///     Describes information for a single antag.
    /// </summary>
    [Prototype("antag")]
    public class AntagPrototype : IPrototype
    {
        private string _name;

        [DataField("id")]
        public string ID { get; private set; }

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
        ///     The antag's objective, displayed at round-start to the player.
        /// </summary>
        [DataField("objective")]
        public string Objective { get; private set; }

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
