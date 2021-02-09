#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components
{
    public partial class ConfigurationComponentData
    {
        [DataClassTarget("config")] public Dictionary<string, string>? Config;

        [DataClassTarget("validation")]
        public Regex? Validation;
        public void ExposeData(ObjectSerializer serializer)
        {
            Config ??= new();
            serializer.DataReadWriteFunction("keys", new List<string>(),
                (list) =>
                {
                    for (var index = 0; index < list.Count; index++)
                    {
                        Config.Add(list[index], "");
                    }
                },
                () => Config.Keys.ToList());
            if (Config.Count == 0) Config = null;

            serializer.DataReadWriteFunction("validation", null,
                val => Validation = val != null ? new Regex(val, RegexOptions.Compiled) : null,
                () => Validation?.ToString());
        }
    }
}
