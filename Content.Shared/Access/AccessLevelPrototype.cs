using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Access
{
    /// <summary>
    ///     Defines a single access level that can be stored on ID cards and checked for.
    /// </summary>
    [Prototype("accessLevel")]
    public class AccessLevelPrototype : IPrototype
    {
        public void LoadFrom(YamlMappingNode mapping)
        {
            ID = mapping.GetNode("id").AsString();
            if (mapping.TryGetNode("name", out var nameNode))
            {
                Name = nameNode.AsString();
            }
            else
            {
                Name = ID;
            }

            Name = Loc.GetString(Name);
        }

        public string ID { get; private set; }

        /// <summary>
        ///     The player-visible name of the access level, in the ID card console and such.
        /// </summary>
        public string Name { get; private set; }
    }
}
