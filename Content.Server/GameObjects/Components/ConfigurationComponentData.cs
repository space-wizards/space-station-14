using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components
{
    public partial class ConfigurationComponentData
    {
        [CustomYamlField("config")] public readonly Dictionary<string, string> Config = new();

        [CustomYamlField("validation")]
        public Regex Validation;
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadWriteFunction("keys", new List<string>(),
                (list) =>
                {
                    for (var index = 0; index < list.Count; index++)
                    {
                        Config.Add(list[index], "");
                    }
                },
                () => Config.Keys.ToList());

            serializer.DataReadFunction("validation", "^[a-zA-Z0-9 ]*$",
                value => Validation = new Regex(value, RegexOptions.Compiled));
        }
    }
}
