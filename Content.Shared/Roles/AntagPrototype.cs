using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
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
        public string ID { get; private set; }

        /// <summary>
        ///     The name of this antag as displayed to players.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        ///     The antag's objective, displayed at round-start to the player.
        /// </summary>
        public string Objective { get; private set; }

        /// <summary>
        ///     Whether or not the antag role is one of the bad guys.
        /// </summary>
        public bool Antagonist { get; private set; }

        /// <summary>
        ///     Whether or not the player can set the antag role in antag preferences.
        /// </summary>
        public bool SetPreference { get; private set; }

        public void LoadFrom(YamlMappingNode mapping)
        {
            ID = mapping.GetNode("id").AsString();
            Name = Loc.GetString(mapping.GetNode("name").ToString());
            Objective = mapping.GetNode("objective").ToString();
            Antagonist = mapping.GetNode("antagonist").AsBool();
            SetPreference = mapping.GetNode("setPreference").AsBool();
        }
    }
}
